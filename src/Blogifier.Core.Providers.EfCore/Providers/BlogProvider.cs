using Blogifier.Core.Data;
using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.EfCore
{
	public class BlogProvider : IBlogProvider
	{
		private readonly AppDbContext _db;
		private readonly IStorageProvider _storageProvider;

		public BlogProvider(AppDbContext db, IStorageProvider storageProvider)
		{
			_db = db;
			_storageProvider = storageProvider;
		}

		public async Task<BlogItem> GetBlogItem()
		{
			var blog = await _db.Blogs.AsNoTracking().OrderBy(b => b.Id).FirstAsync();
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
                FooterScript = blog.FooterScript
			};
		}

		public async Task<Blog> GetBlog()
		{
			return await _db.Blogs.OrderBy(b => b.Id).AsNoTracking().FirstAsync();
		}

		public async Task<Blog> TryGetBlog()
		{
			return await _db.Blogs.OrderBy(b => b.Id).AsNoTracking().FirstOrDefaultAsync();
		}

		public async Task<List<Category>> GetBlogCategories()
		{
			return await _db.Categories.AsNoTracking().ToListAsync();
		}

        public async Task<bool> AddAsync(Blog blog)
        {
            _db.Blogs.Add(blog);
			return await _db.SaveChangesAsync() > 0;
        }

		public async Task<bool> Update(Blog blog)
		{
			var existing = await _db.Blogs.OrderBy(b => b.Id).FirstAsync();

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

			return await _db.SaveChangesAsync() > 0;
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
