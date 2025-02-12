using Blogifier.Core.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Shared;
using Blogifier.Shared.Extensions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Blogifier.Core.Providers.MongoDb
{
	public class SyndicationProvider : ISyndicationProvider
	{
		private readonly IStorageProvider _storageProvider;
        private readonly IBlogProvider _blogProvider;
        private readonly IMongoCollection<Post> _postCollection;

        private static Guid _userId;
		private static string _webRoot;
		private static Uri _baseUrl;

		public SyndicationProvider(IMongoDatabase db, IStorageProvider storageProvider, IBlogProvider blogProvider)
		{
            _postCollection = db.GetNamedCollection<Post>();
            _storageProvider = storageProvider;
            _blogProvider = blogProvider;
		}

		public async Task<List<Post>> GetPosts(string feedUrl, Guid userId, Uri baseUrl, string webRoot = "/")
		{
			_userId = userId;
			_webRoot = webRoot;
			_baseUrl = baseUrl;

			List<Post> posts = new List<Post>();
			try
			{
				SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(feedUrl));
				foreach (var item in feed.Items)
				{
					posts.Add(await GetPost(item));
				}
			}
			catch (Exception ex)
			{
				Serilog.Log.Error($"Error parsing feed URL: {ex.Message}");
			}
			return posts;
		}

		public async Task<bool> ImportPost(Post post)
		{
			try
			{
				await ImportImages(post);
				await ImportFiles(post);

				var converter = new ReverseMarkdown.Converter();

				post.Description = GetDescription(converter.Convert(post.Description));
				post.Content = converter.Convert(post.Content);
				post.Selected = false;

                try
                {
				    await _postCollection.InsertOneAsync((Post)post);
				}
                catch
				{
					Serilog.Log.Error($"Error saving post {post.Title}");
					return false;
				}

				var savedPost = await _postCollection.Find(p => p.Slug == post.Slug).SingleAsync();
				if(savedPost == null)
				{
					Serilog.Log.Error($"Error finding saved post - {post.Title}");
					return false;
				}

				var blog = await _blogProvider.GetBlog();
                var updateDefinition = Builders<Post>.Update
                    .Set(p => p.Blog, blog)
                    .Set(p => p.BlogId, blog.Id);

                var result = await _postCollection.UpdateOneAsync(p => p.Id == post.Id, updateDefinition);

                return result.IsAcknowledged && result.ModifiedCount > 0;
			}
			catch (Exception ex)
			{
				Serilog.Log.Error($"Error importing post {post.Title}: {ex.Message}");
				return false;
			}
		}

		#region Private members

		async Task<Post> GetPost(SyndicationItem syndicationItem)
		{
			var post = new Post()
			{
                Id = Guid.NewGuid(),
				AuthorId = _userId,
				PostType = PostType.Post,
				Title = syndicationItem.Title.Text,
				Slug = await GetSlug(syndicationItem.Title.Text),
				Description = GetDescription(syndicationItem.Title.Text),
				Content = syndicationItem.Summary.Text,
				Cover = Constants.DefaultCover,
				Published = syndicationItem.PublishDate.DateTime,
				DateCreated = syndicationItem.PublishDate.DateTime,
				DateUpdated = syndicationItem.LastUpdatedTime.DateTime
			};

			if (syndicationItem.ElementExtensions != null)
			{
				foreach (SyndicationElementExtension ext in syndicationItem.ElementExtensions)
				{
					if (ext.GetObject<XElement>().Name.LocalName == "summary")
						post.Description = GetDescription(ext.GetObject<XElement>().Value);

					if (ext.GetObject<XElement>().Name.LocalName == "cover")
						post.Cover = ext.GetObject<XElement>().Value;
				}
			}

            if (syndicationItem.Categories != null)
            {
                if (post.Categories == null)
                {
                    post.Categories = new List<Category>();
                }

                foreach (var category in syndicationItem.Categories)
                {
                    post.Categories.Add(new Category
                    {
                        Id = Guid.NewGuid(),
                        Content = category.Name,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    });
                }
            }

            return post;
		}

		async Task ImportImages(Post post)
		{
			string rgx = @"<img[^>]*?src\s*=\s*[""']?([^'"" >]+?)[ '""][^>]*?>";

			if (string.IsNullOrEmpty(post.Content))
				return;

			if(post.Cover != Constants.DefaultCover)
			{
				var path = string.Format("{0}/{1}/{2}", post.AuthorId, post.Published.Year, post.Published.Month);

				var mdTag = await _storageProvider.UploadFromWeb(new Uri(post.Cover), _webRoot, path);
				if (mdTag.Length > 0 && mdTag.IndexOf("(") > 2)
					post.Cover = mdTag.Substring(mdTag.IndexOf("(") + 2).Replace(")", "");
			}

			var matches = Regex.Matches(post.Content, rgx, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			if (matches == null)
				return;

			foreach (Match m in matches)
			{
				try
				{
					var tag = m.Groups[0].Value;
					var path = string.Format("{0}/{1}/{2}", post.AuthorId, post.Published.Year, post.Published.Month);

					var uri = Regex.Match(tag, "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase).Groups[1].Value;
					uri = ValidateUrl(uri);
					var mdTag = "";

					if (uri.Contains("data:image"))
						mdTag = await _storageProvider.UploadBase64Image(uri, _webRoot, path);
					else
					{
						try
						{
							mdTag = await _storageProvider.UploadFromWeb(new Uri(uri), _webRoot, path);
						}
						catch
						{
							if (uri.StartsWith("https:"))
							{
								mdTag = await _storageProvider.UploadFromWeb(new Uri(uri.Replace("https:", "http:")), _webRoot, path);
							}
						}
					}
					post.Content = post.Content.ReplaceIgnoreCase(tag, mdTag);
				}
				catch (Exception ex)
				{
					Serilog.Log.Error($"Error importing images: {ex.Message}");
				}
			}
		}

		async Task ImportFiles(Post post)
		{
			var rgx = @"(?i)<a\b[^>]*?>(?<text>.*?)</a>";
			string[] exts = new string[] { "zip", "7z", "xml", "pdf", "doc", "docx", "xls", "xlsx", "mp3", "mp4", "avi" };

			if (string.IsNullOrEmpty(post.Content))
				return;

			MatchCollection matches = Regex.Matches(post.Content, rgx, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			if (matches != null)
			{
				foreach (Match m in matches)
				{
					try
					{
						var tag = m.Value;
						var src = XElement.Parse(tag).Attribute("href").Value;
						var mdTag = "";

						foreach (var ext in exts)
						{
							if (src.ToLower().EndsWith($".{ext}"))
							{
								var uri = ValidateUrl(src);
								var path = string.Format("{0}/{1}/{2}", post.AuthorId, post.Published.Year, post.Published.Month);

								mdTag = await _storageProvider.UploadFromWeb(new Uri(uri), _webRoot, path);

								if (mdTag.StartsWith("!"))
									mdTag = mdTag.Substring(1);

								post.Content = post.Content.ReplaceIgnoreCase(m.Value, mdTag);
							}
						}
					}
					catch (Exception ex)
					{
						Serilog.Log.Error($"Error importing files: {ex.Message}");
					}
				}
			}
		}

		async Task<string> GetSlug(string title)
		{
			string slug = title.ToSlug();
			Post post = await _postCollection.Find(p => p.Slug == slug).SingleOrDefaultAsync();

			if (post == null)
				return slug;

			for (int i = 2; i < 100; i++)
			{
				post = await _postCollection.Find(p => p.Slug == $"{slug}{i}").SingleAsync();

				if (post == null)
					return slug + i.ToString();
			}
			return slug;
		}

		string ValidateUrl(string link)
		{
			var url = link;

			var baseUrl = _baseUrl.ToString();
			if (baseUrl.EndsWith("/"))
				baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);

			if (url.StartsWith("~"))
				url = url.Replace("~", baseUrl);

			if (url.StartsWith("/"))
				url = $"{baseUrl}{url}";

			if (!(url.StartsWith("http:") || url.StartsWith("https:")))
				url = $"{baseUrl}/{url}";

			return url;
		}

		string GetDescription(string description)
		{
			description = description.StripHtml();
			if (description.Length > 450)
				description = description.Substring(0, 446) + "...";
			return description;
		}

		#endregion
	}
}
