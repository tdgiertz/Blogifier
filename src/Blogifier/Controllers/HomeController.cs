using Blogifier.Core.Extensions;
using Blogifier.Core.Providers;
using Blogifier.Shared;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Serilog;

namespace Blogifier.Controllers
{
    public class HomeController : Controller
    {
        protected readonly IBlogProvider _blogProvider;
        protected readonly IPostProvider _postProvider;
        protected readonly IFeedProvider _feedProvider;
        protected readonly IAuthorProvider _authorProvider;
        protected readonly IThemeProvider _themeProvider;
        protected readonly IStorageProvider _storageProvider;
        protected readonly ICompositeViewEngine _compositeViewEngine;

        public HomeController(IBlogProvider blogProvider,
            IPostProvider postProvider, IFeedProvider feedProvider, IAuthorProvider authorProvider, IThemeProvider themeProvider,
            IStorageProvider storageProvider, ICompositeViewEngine compositeViewEngine)
        {
            _blogProvider = blogProvider;
            _postProvider = postProvider;
            _feedProvider = feedProvider;
            _authorProvider = authorProvider;
            _themeProvider = themeProvider;
            _storageProvider = storageProvider;
            _compositeViewEngine = compositeViewEngine;
        }

        public async Task<IActionResult> Index()
        {
            var model = await GetBlogPosts();

            //If no blogs are setup redirect to first time registration
            if (model == null)
            {
                return Redirect("~/admin/register");
            }

            if(model.PagingDescriptor != null)
            {
                model.PagingDescriptor.PagingUrl = "/partial/" + model.PagingDescriptor.LastDateTime?.ToString("o");
            }

            return View($"~/Views/Themes/{model.Blog.Theme}/Index.cshtml", model);
        }

        [HttpGet("/partial/{publishedDate:DateTime}")]
        public async Task<IActionResult> IndexPartial(DateTime? publishedDate)
        {
            var model = await GetBlogPosts(publishedDate: publishedDate);

            if(model?.PagingDescriptor != null)
            {
                model.PagingDescriptor.PagingUrl = "/partial/" + model.PagingDescriptor.LastDateTime?.ToString("o");
            }

            return PartialView($"~/Views/Themes/{model.Blog.Theme}/post/post-grid-items.cshtml", model);
        }

        [HttpGet("/{slug}")]
        public async Task<IActionResult> Index(string slug)
        {
            if (!string.IsNullOrEmpty(slug))
            {
                return await GetSingleBlogPost(slug);
            }
            return Redirect("~/");
        }

        [HttpGet("/admin")]
        public async Task<IActionResult> Admin()
        {
            return await Task.FromResult(File("~/index.html", "text/html"));
        }

        [HttpPost]
        public async Task<IActionResult> Search(string term)
        {

            if (!string.IsNullOrEmpty(term))
            {
                var model = await GetBlogPosts(term);

                if(model.PagingDescriptor != null)
                {
                    model.PagingDescriptor.LastDateTime = model.Posts.LastOrDefault()?.Published;
                    model.PagingDescriptor.PagingUrl = "/search/" + model.PagingDescriptor.LastDateTime?.ToString("o") + "/" + term;
                }

                string viewPath = $"~/Views/Themes/{model.Blog.Theme}/Search.cshtml";
                if (IsViewExists(viewPath))
                    return View(viewPath, model);
                else
                    return Redirect("~/home");
            }
            else
            {
                return Redirect("~/home");
            }
        }

        [HttpGet("/search/{publishedDate:DateTime}/{term}")]
        public async Task<IActionResult> SearchPartial(DateTime? publishedDate, string term)
        {
            var model = await GetBlogPosts(publishedDate: publishedDate, term: term);

            if(model?.PagingDescriptor != null)
            {
                model.PagingDescriptor.LastDateTime = model.Posts.LastOrDefault()?.Published;
                model.PagingDescriptor.PagingUrl = "/search/" + model.PagingDescriptor.LastDateTime?.ToString("o") + "/" + term;
            }

            return PartialView($"~/Views/Themes/{model.Blog.Theme}/post/post-list-items.cshtml", model);
        }

        [HttpGet("categories/{category}")]
        public async Task<IActionResult> Categories(string category)
        {
            var model = await GetBlogPosts("", category);
            string viewPath = $"~/Views/Themes/{model.Blog.Theme}/Category.cshtml";

            if(model?.PagingDescriptor != null)
            {
                model.PagingDescriptor.LastDateTime = model.Posts.LastOrDefault()?.Published;
                model.PagingDescriptor.PagingUrl = "/categories/" + model.PagingDescriptor.LastDateTime?.ToString("o") + "/" + category;
            }

            ViewBag.Category = category;

            if (IsViewExists(viewPath))
                return View(viewPath, model);

            return View($"~/Views/Themes/{model.Blog.Theme}/Index.cshtml", model);
        }

        [HttpGet("/categories/{publishedDate:DateTime}/{category}")]
        public async Task<IActionResult> CategoriesPartial(DateTime? publishedDate, string category)
        {
            var model = await GetBlogPosts(publishedDate: publishedDate, category: category);

            if(model?.PagingDescriptor != null)
            {
                model.PagingDescriptor.LastDateTime = model.Posts.LastOrDefault()?.Published;
                model.PagingDescriptor.PagingUrl = "/categories/" + model.PagingDescriptor.LastDateTime?.ToString("o") + "/" + category;
            }

            return PartialView($"~/Views/Themes/{model.Blog.Theme}/post/post-list-items.cshtml", model);
        }

        [HttpGet("posts/{slug}")]
        public async Task<IActionResult> Single(string slug)
        {
            return await GetSingleBlogPost(slug);
        }

        [HttpGet("error")]
        public async Task<IActionResult> Error()
        {
            try
            {
                PostModel model = new PostModel();
                model.Blog = await _blogProvider.GetBlogItem();
                string viewPath = $"~/Views/Themes/{model.Blog.Theme}/404.cshtml";
                if (IsViewExists(viewPath))
                    return View(viewPath, model);
                return View($"~/Views/Error.cshtml");
            }
            catch
            {
                return View($"~/Views/Error.cshtml");
            }
        }

        [ResponseCache(Duration = 1200)]
        [HttpGet("feed/{type}")]
        public async Task<IActionResult> Rss(string type)
        {
            string host = Request.Scheme + "://" + Request.Host;
            var blog = await _blogProvider.GetBlog();

            var posts = await _feedProvider.GetEntries(type, host);
            var items = new List<SyndicationItem>();

            var feed = new SyndicationFeed(
                 blog.Title,
                 blog.Description,
                 new Uri(host),
                 host,
                 posts.FirstOrDefault().Published
            );

            if (posts != null && posts.Count() > 0)
            {
                foreach (var post in posts)
                {
                    var item = new SyndicationItem(
                         post.Title,
                         post.Description.MdToHtml(),
                         new Uri(post.Id),
                         post.Id,
                         post.Published
                    );
                    item.PublishDate = post.Published;
                    items.Add(item);
                }
            }
            feed.Items = items;

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true
            };

            using (var stream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(stream, settings))
                {
                    var rssFormatter = new Rss20FeedFormatter(feed, false);
                    rssFormatter.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                }
                return File(stream.ToArray(), "application/xml; charset=utf-8");
            }
        }

        private bool IsViewExists(string viewPath)
        {
            var result = _compositeViewEngine.GetView("", viewPath, false);
            return result.Success;
        }

        public async Task<IActionResult> GetSingleBlogPost(string slug)
        {
            try
            {
                ViewBag.Slug = slug;
                PostModel model = await _postProvider.GetPostModel(slug);

                // If unpublished and unauthorised redirect to error / 404.
                if (model.Post.Published == DateTime.MinValue && !User.Identity.IsAuthenticated)
                {
                    return Redirect("~/error");
                }

                model.Blog = await _blogProvider.GetBlogItem();
                model.Post.Description = model.Post.Description.MdToHtml();
                model.Post.Content = model.Post.Content.MdToHtml();

                if (model.Post.PostType == PostType.Page)
                {
                    string viewPath = $"~/Views/Themes/{model.Blog.Theme}/Page.cshtml";
                    if (IsViewExists(viewPath))
                    {
                        return View(viewPath, model);
                    }
                }

                return View($"~/Views/Themes/{model.Blog.Theme}/Post.cshtml", model);
            }
            catch(Exception ex)
            {
                Log.Error("Error getting blog post", ex);
                return Redirect("~/error");
            }
        }

        public async Task<ListModel> GetBlogPosts(string term = "", string category = "", string slug = "", DateTime? publishedDate = null)
        {
            var model = new ListModel { };

            try
            {
                model.Blog =await  _blogProvider.GetBlogItem();
            }
            catch
            {
                return null;
            }

            model.PagingDescriptor = new InfinitePagingDescriptor { PageSize = 9, LastDateTime = publishedDate };

            if (!string.IsNullOrEmpty(category))
            {
                model.PostListType = PostListType.Category;
                model.Posts = await _postProvider.GetPublishedListAsync(model.PagingDescriptor, category);
            }
            else if (string.IsNullOrEmpty(term))
            {
                model.PostListType = PostListType.Blog;
                if (model.Blog.IncludeFeatured)
                {
                    model.Posts = await _postProvider.GetPublishedListAsync(model.PagingDescriptor);
                    model.FeaturedPosts = await _postProvider.GetFeaturedListAsync(new InfinitePagingDescriptor { PageSize = 3 });
                }
                else
                {
                    model.Posts = await _postProvider.GetPublishedListAsync(model.PagingDescriptor);
                }
            }
            else
            {
                ViewBag.SearchTerm = term;
                model.PostListType = PostListType.Search;
                model.PagingDescriptor.SearchTerm = term;
                model.Posts = await _postProvider.GetPublishedListAsync(model.PagingDescriptor);
            }

            model.PagingDescriptor.LastDateTime = model.Posts.LastOrDefault()?.Published;

            return model;
        }
    }
}
