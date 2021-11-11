using Blogifier.Core.Data;
using Blogifier.Core.Extensions;
using Blogifier.Shared;
using Blogifier.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.EfCore
{
	public class PostProvider : IPostProvider
	{
		private readonly AppDbContext _db;
        private readonly ICategoryProvider _categoryProvider;
        private readonly IConfiguration _configuration;

        public PostProvider(AppDbContext db, ICategoryProvider categoryProvider, IConfiguration configuration)
		{
			_db = db;
            _categoryProvider = categoryProvider;
            _configuration = configuration;
		}

		public async Task<List<Post>> GetPosts(PublishedStatus filter, PostType postType)
		{
			switch (filter)
			{
				case PublishedStatus.Published:
					return await _db.Posts.AsNoTracking().Where(p => p.PostType == postType).Where(p => p.Published > DateTime.MinValue).OrderByDescending(p => p.Published).ToListAsync();
				case PublishedStatus.Drafts:
					return await _db.Posts.AsNoTracking().Where(p => p.PostType == postType).Where(p => p.Published == DateTime.MinValue).OrderByDescending(p => p.Id).ToListAsync();
				case PublishedStatus.Featured:
					return await _db.Posts.AsNoTracking().Where(p => p.PostType == postType).Where(p => p.IsFeatured).OrderByDescending(p => p.Id).ToListAsync();
				default:
					return await _db.Posts.AsNoTracking().Where(p => p.PostType == postType).OrderByDescending(p => p.Id).ToListAsync();
			}
		}

		public async Task<List<Post>> SearchPosts(string term)
		{
			if (term == "*")
				return await _db.Posts.ToListAsync();

			return await _db.Posts
				.AsNoTracking()
				.Where(p => p.Title.ToLower().Contains(term.ToLower()))
				.ToListAsync();
		}

		public async Task<IEnumerable<PostItem>> Search(PagingDescriptor pagingDescriptor, string term, Guid author = default(Guid), string include = "", bool sanitize = false)
		{
            term = term.ToLower();

			var results = new List<SearchResult>();
			var termList = term.ToLower().Split(' ').ToList();
            var categories = await _db.Categories.ToListAsync();

			foreach (var p in await GetPosts(null, include, author))
			{
				var rank = 0;
				var hits = 0;

				foreach (var termItem in termList)
				{
					if (termItem.Length < 4 && rank > 0) continue;

					if (p.Categories != null && p.Categories.Count > 0)
					{
                        foreach (var category in p.Categories)
                        {
                            if (category.Content.ToLower() == termItem) rank += 10;
                        }
                    }
					if (p.Title.ToLower().Contains(termItem))
					{
						hits = Regex.Matches(p.Title.ToLower(), termItem).Count;
						rank += hits * 10;
					}
					if (p.Description.ToLower().Contains(termItem))
					{
						hits = Regex.Matches(p.Description.ToLower(), termItem).Count;
						rank += hits * 3;
					}
					if (p.Content.ToLower().Contains(termItem))
					{
						rank += Regex.Matches(p.Content.ToLower(), termItem).Count;
					}
				}
				if (rank > 0)
				{
					results.Add(new SearchResult { Rank = rank, Item = PostToItem(p, sanitize) });
				}
			}

			results = results.OrderByDescending(r => r.Rank).ToList();

			var posts = new List<PostItem>();
			for (int i = 0; i < results.Count; i++)
			{
				posts.Add(results[i].Item);
			}
			pagingDescriptor.SetTotalCount(posts.Count);
			return await Task.Run(() => posts.Skip(pagingDescriptor.Skip).Take(pagingDescriptor.PageSize).ToList());
		}

		public async Task<Post> GetPostById(Guid id)
		{
			return await _db.Posts.Where(p => p.Id == id).FirstOrDefaultAsync();
		}

		public async Task<IEnumerable<PostItem>> GetPostItems()
		{
			var posts = await _db.Posts.ToListAsync();
			var postItems = new List<PostItem>();

			foreach (var post in posts)
			{
				postItems.Add(new PostItem
				{
					Id = post.Id,
					Title = post.Title,
					Description = post.Description,
					Content = post.Content,
					Slug = post.Slug,
					Author = _db.Authors.Where(a => a.Id == post.AuthorId).First(),
					Cover = string.IsNullOrEmpty(post.Cover) ? Constants.DefaultCover : post.Cover,
					Published = post.Published,
					PostViews = post.PostViews,
					Featured = post.IsFeatured
				});
			}

			return postItems;
		}

		public async Task<PostModel> GetPostModel(string slug)
        {
            var model = new PostModel();

            var post = await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.Categories)
                .AsNoTracking()
                .Where(p => p.Slug == slug)
                .FirstOrDefaultAsync();

            if(post == null)
            {
                return model;
            }

            post.PostViews++;
            await _db.SaveChangesAsync();

            var previousPost = await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.Categories)
                .Where(p => p.Published > DateTime.MinValue && p.Published < post.Published)
                .OrderByDescending(p => p.Published)
                .FirstOrDefaultAsync();
            var nextPost = await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.Categories)
                .Where(p => p.Published > DateTime.MinValue && p.Published > post.Published)
                .OrderBy(p => p.Published)
                .FirstOrDefaultAsync();

            model.Post = PostToItem(post);
            model.Older = PostToItem(previousPost);
            model.Newer = PostToItem(nextPost);

            model.Related = await Search(new Pager(1), model.Post.Title, default(Guid), "PF", true);
            model.Related = model.Related.Where(r => r.Id != model.Post.Id).ToList();

            return model;
        }

        public async Task<Post> GetPostBySlug(string slug)
		{
			return await _db.Posts.Where(p => p.Slug == slug).FirstOrDefaultAsync();
		}

		public async Task<string> GetSlugFromTitle(string title)
		{
			string slug = title.ToSlug();
			var post = _db.Posts.Where(p => p.Slug == slug).FirstOrDefault();

			if (post != null)
			{
				for (int i = 2; i < 100; i++)
				{
					slug = $"{slug}{i}";
					if (_db.Posts.Where(p => p.Slug == slug).FirstOrDefault() == null)
					{
						return await Task.FromResult(slug);
					}
				}
			}
			return await Task.FromResult(slug);
		}

		public async Task<bool> Add(Post post)
		{
			var existing = await _db.Posts.Where(p => p.Slug == post.Slug).FirstOrDefaultAsync();
			if (existing != null)
				return false;

			post.Blog = _db.Blogs.First();
			post.DateCreated = DateTime.UtcNow;

            // sanitize HTML fields
            post.Content = post.Content.RemoveScriptTags();
            post.Description = post.Description.RemoveScriptTags();

			await _db.Posts.AddAsync(post);
			return await _db.SaveChangesAsync() > 0;
		}

		public async Task<bool> Update(Post post)
		{
			var existing = await _db.Posts.Where(p => p.Slug == post.Slug).FirstOrDefaultAsync();
			if (existing == null)
				return false;

			existing.Slug = post.Slug;
			existing.Title = post.Title;
			existing.Description = post.Description.RemoveScriptTags();
			existing.Content = post.Content.RemoveScriptTags();
			existing.Cover = post.Cover;
			existing.PostType = post.PostType;
			existing.Published = post.Published;

			return await _db.SaveChangesAsync() > 0;
		}

		public async Task<bool> Publish(Guid id, bool publish)
		{
			var existing = await _db.Posts.Where(p => p.Id == id).FirstOrDefaultAsync();
			if (existing == null)
				return false;

			existing.Published = publish ? DateTime.UtcNow : DateTime.MinValue;

			return await _db.SaveChangesAsync() > 0;
		}

		public async Task<bool> Featured(Guid id, bool featured)
		{
			var existing = await _db.Posts.Where(p => p.Id == id).FirstOrDefaultAsync();
			if (existing == null)
				return false;

			existing.IsFeatured = featured;

			return await _db.SaveChangesAsync() > 0;
		}

		public async Task<IEnumerable<PostItem>> GetList(PagingDescriptor pagingDescriptor, Guid author = default(Guid), string category = "", string include = "", bool sanitize = true)
		{
			var posts = await GetPosts(pagingDescriptor, include, author, category);

			var items = new List<PostItem>();
			foreach (var post in posts)
			{
				items.Add(PostToItem(post, sanitize));
			}

			return items;
		}

        private async Task<List<Post>> GetPostsCategoryContentAsync(string content)
        {
            return await _db.Posts.Include(p => p.Categories).Where(c => c.Content.ToLower() == content.ToLower()).ToListAsync();
        }

		public async Task<IEnumerable<PostItem>> GetPopular(PagingDescriptor pagingDescriptor, Guid author = default(Guid))
		{
			var posts = new List<Post>();

			if (author != default(Guid))
				posts = _db.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Categories)
                    .AsNoTracking()
                    .Where(p => p.Published > DateTime.MinValue && p.AuthorId == author)
				    .OrderByDescending(p => p.PostViews)
                    .ThenByDescending(p => p.Published)
                    .ToList();
			else
				posts = _db.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Categories)
                    .AsNoTracking()
                    .Where(p => p.Published > DateTime.MinValue)
					.OrderByDescending(p => p.PostViews)
                    .ThenByDescending(p => p.Published)
                    .ToList();

			pagingDescriptor.SetTotalCount(posts.Count);

			var items = new List<PostItem>();
			foreach (var p in posts.Skip(pagingDescriptor.Skip).Take(pagingDescriptor.PageSize).ToList())
			{
				items.Add(PostToItem(p, true));
			}
			return await Task.FromResult(items);
		}

		public async Task<bool> Remove(Guid id)
		{
			var existing = await _db.Posts.Where(p => p.Id == id).FirstOrDefaultAsync();
			if (existing == null)
				return false;

			_db.Posts.Remove(existing);
			await _db.SaveChangesAsync();
			return true;
		}

		#region Private methods

		private static PostItem PostToItem(Post post, bool sanitize = false)
		{
            if(post == null)
            {
                return null;
            }

			var postItem = new PostItem
			{
				Id = post.Id,
                PostType = post.PostType,
				Slug = post.Slug,
				Title = post.Title,
				Description = post.Description,
				Content = post.Content,
				Categories = post.Categories,
				Cover = post.Cover,
				PostViews = post.PostViews,
				Rating = post.Rating,
				Published = post.Published,
				Featured = post.IsFeatured,
				Author = post.Author,
				SocialFields = new List<SocialField>()
			};

			if (postItem.Author != null)
			{
				if(string.IsNullOrEmpty(postItem.Author.Avatar))
                    string.Format(Constants.AvatarDataImage, postItem.Author.DisplayName.Substring(0, 1).ToUpper());

                postItem.Author.Email = sanitize ? "donotreply@us.com" : postItem.Author.Email;
			}

			return postItem;
		}

        private async Task<List<Post>> GetPosts(PagingDescriptor pagingDescriptor, string include, Guid author, string category = null)
		{
            IQueryable<Post> query = _db.Posts.Include(p => p.Categories).Include(p => p.Author);

            var expression = PredicateBuilder.True<Post>();

            if(!string.IsNullOrEmpty(category))
            {
                expression = expression.And(p => p.Categories.Any(c => c.Content == category));
            }

            Expression<Func<Post, bool>> innerExpression = null;

            if (include.ToUpper().Contains(Constants.PostDraft) || string.IsNullOrEmpty(include))
			{
				innerExpression = author != default(Guid)
                    ? OrExpression(innerExpression, p => p.Published == DateTime.MinValue && p.AuthorId == author && p.PostType == PostType.Post)
                    : OrExpression(innerExpression, p => p.Published == DateTime.MinValue && p.PostType == PostType.Post);
			}

			if (include.ToUpper().Contains(Constants.PostFeatured) || string.IsNullOrEmpty(include))
			{
				innerExpression = author != default(Guid)
                    ? OrExpression(innerExpression, p => p.Published > DateTime.MinValue && p.IsFeatured && p.AuthorId == author && p.PostType == PostType.Post)
                    : OrExpression(innerExpression, p => p.Published > DateTime.MinValue && p.IsFeatured && p.PostType == PostType.Post);
			}

			if (include.ToUpper().Contains(Constants.PostPublished) || string.IsNullOrEmpty(include))
			{
				innerExpression = author != default(Guid)
                    ? OrExpression(innerExpression, p => p.Published > DateTime.MinValue && !p.IsFeatured && p.AuthorId == author && p.PostType == PostType.Post)
                    : OrExpression(innerExpression, p => p.Published > DateTime.MinValue && !p.IsFeatured && p.PostType == PostType.Post);
			}

            if(innerExpression != null)
            {
                expression = expression.And(innerExpression);
            }

            query = query.Where(expression).OrderByDescending(p => p.Published);

            if(pagingDescriptor != null)
            {
                return await PaginatedList<Post>.CreateAsync(query, pagingDescriptor);
            }
            else
            {
                return await query.ToListAsync();
            }
		}

        private static Expression<Func<Post, bool>> OrExpression(Expression<Func<Post, bool>> expression, Expression<Func<Post, bool>> orExpression)
        {
            if(expression == null)
            {
                return PredicateBuilder.Create<Post>(orExpression);
            }

            return expression.Or(orExpression);
        }

        bool IsDemo()
        {
            return _configuration.GetSection("Blogifier").GetValue<bool>("DemoMode");
        }

		#endregion
	}
}
