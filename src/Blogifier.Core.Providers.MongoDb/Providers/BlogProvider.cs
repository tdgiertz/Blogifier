using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    public class BlogProvider : IBlogProvider
    {
        private readonly IMongoCollection<MongoBlog> _blogCollection;
        private readonly IStorageProvider _storageProvider;
        private readonly ICategoryProvider _categoryProvider;

        public BlogProvider(IMongoDatabase db, IStorageProvider storageProvider, ICategoryProvider categoryProvider)
        {
            _blogCollection = db.GetNamedCollection<MongoBlog>();
            _storageProvider = storageProvider;
            _categoryProvider = categoryProvider;
        }

        public async Task<BlogItem> GetBlogItem()
        {
            var blog = await GetBlog();
            blog.Theme = blog.Theme.ToLower();
            return new BlogItem
            {
                Title = blog.Title,
                Description = blog.Description,
                Theme = blog.Theme,
                IncludeFeatured = blog.IncludeFeatured,
                ItemsPerPage = blog.ItemsPerPage,
                SocialFields = new List<SocialField>(),
                Cover = string.IsNullOrEmpty(blog.Cover) ? blog.Cover : Constants.DefaultCover,
                Logo = string.IsNullOrEmpty(blog.Logo) ? blog.Logo : Constants.DefaultLogo,
                HeaderScript = blog.HeaderScript,
                FooterScript = blog.FooterScript,
                values = await GetValues(blog.Theme)
            };
        }

        public async Task<Blog> GetBlog()
        {
            return await _blogCollection.Find(_ => true).SortBy(b => b.Id).FirstAsync();
        }

        public async Task<Blog> TryGetBlog()
        {
            return await _blogCollection.Find(_ => true).SortBy(b => b.Id).FirstOrDefaultAsync();
        }

        public async Task<List<Category>> GetBlogCategories()
        {
            return await _categoryProvider.GetCategoriesAsync();
        }

        public async Task<bool> Update(Blog blog)
        {
            var existing = await _blogCollection.Find(_ => true).SortBy(b => b.Id).FirstAsync();

            existing.Title = blog.Title;
            existing.Description = blog.Description;
            existing.ItemsPerPage = blog.ItemsPerPage;
            existing.IncludeFeatured = blog.IncludeFeatured;
            existing.Theme = blog.Theme;
            existing.Cover = blog.Cover;
            existing.Logo = blog.Logo;
            existing.HeaderScript = blog.HeaderScript;
            existing.FooterScript = blog.FooterScript;
            existing.AnalyticsListType = blog.AnalyticsListType;
            existing.AnalyticsPeriod = blog.AnalyticsPeriod;

            var result = await _blogCollection.ReplaceOneAsync(_ => true, existing);

            return result.IsAcknowledged;
        }

        public async Task<bool> AddAsync(Blog blog)
        {
            await _blogCollection.InsertOneAsync((MongoBlog)blog);

            return true;
        }

        private async Task<dynamic> GetValues(string theme)
        {
            var settings = await _storageProvider.GetThemeSettings(theme);
            var values = new Dictionary<string, string>();

            if (settings != null && settings.Sections != null)
            {
                foreach (var section in settings.Sections)
                {
                    if (section.Fields != null)
                    {
                        foreach (var field in section.Fields)
                        {
                            values.Add(field.Id, field.Value);
                        }
                    }
                }
            }
            return values;
        }
    }
}
