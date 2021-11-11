using Blogifier.Core.Data;
using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.EfCore
{
    public class CategoryProvider : ICategoryProvider
    {
        private readonly AppDbContext _db;

        public CategoryProvider(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CategoryItem>> Categories()
        {
            if (_db.Posts != null && _db.Posts.Count() > 0)
            {
                var categories = await _db.Categories
                    .AsNoTracking()
                    .Select(c => new CategoryItem
                    {
                        Selected = false,
                        Id = c.Id,
                        Category = c.Content.ToLower(),
                        PostCount = c.Posts.Count(),
                        DateCreated = c.DateCreated
                    })
                    .OrderBy(c => c.Category)
                    .ToListAsync();

                return categories;
            }

            return await Task.FromResult(new List<CategoryItem>());
        }

        public async Task<List<CategoryItem>> GetPublishedCategories()
        {
            if (_db.Posts != null && _db.Posts.Count() > 0)
            {
                var categories = await _db.Categories
                    .Include(c => c.Posts)
                    .Where(c => c.Posts.Any(p => p.Published != DateTime.MinValue))
                    .AsNoTracking()
                    .Select(c => new CategoryItem
                    {
                        Selected = false,
                        Id = c.Id,
                        Category = c.Content.ToLower(),
                        PostCount = c.Posts.Count(),
                        DateCreated = c.DateCreated
                    })
                    .OrderBy(c => c.Category)
                    .ToListAsync();

                return categories;
            }

            return await Task.FromResult(new List<CategoryItem>());
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            if (_db.Posts != null && _db.Posts.Count() > 0)
            {
                var categories = await _db.Categories
                    .AsNoTracking()
                    .ToListAsync();

                return categories;
            }

            return await Task.FromResult(new List<Category>());
        }

        public async Task<List<CategoryItem>> SearchCategories(string term)
        {
            var categories = await Categories();

            if (term == "*")
            {
                return categories;
            }

            return categories.Where(c => c.Category.ToLower().Contains(term.ToLower())).ToList();
        }

        public async Task<Category> GetCategory(Guid categoryId)
        {
            return await _db.Categories.AsNoTracking()
                .Where(c => c.Id == categoryId)
                .FirstOrDefaultAsync();
        }

        public async Task<ICollection<Category>> GetPostCategories(Guid postId)
        {
            var post = await _db.Posts.AsNoTracking()
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == postId);

            return post?.Categories ?? new List<Category>();
        }

        public async Task<bool> SaveCategory(Category category)
        {
            var dbCategory = await _db.Categories.Where(c => c.Id == category.Id).FirstOrDefaultAsync();
            if (dbCategory == null)
                return false;

            dbCategory.Content = category.Content;
            dbCategory.Description = category.Description;
            dbCategory.DateUpdated = DateTime.UtcNow;

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<Category> SaveCategory(string tag)
        {
            var category = await _db.Categories
                .Where(c => c.Content == tag)
                .FirstOrDefaultAsync();

            if (category != null)
                return category;

            category = new Category()
            {
                Id = Guid.NewGuid(),
                Content = tag,
                DateCreated = DateTime.UtcNow
            };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return category;
        }

        public async Task<bool> AddPostCategory(Guid postId, string tag)
        {
            var category = await SaveCategory(tag);

            if (category == null)
                return false;

            var post = await _db.Posts.Where(p => p.Id == postId).FirstOrDefaultAsync();
            if (post == null)
                return false;

            if (post.Categories == null)
                post.Categories = new List<Category>();

            post.Categories.Add(category);

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> SavePostCategories(Guid postId, List<Category> categories)
        {
            var post = await _db.Posts
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == postId);

            var existingCategories = (await _db.Categories
                .Where(c => categories.Select(cat => cat.Content).Any(content => content == c.Content))
                .ToListAsync()).ToDictionary(k => k.Content, v => v);

            foreach(var category in categories)
            {
                if(post.Categories.Any(c => c.Content == category.Content)) continue;
                if(existingCategories.TryGetValue(category.Content, out var existingCategory))
                {
                    post.Categories.Add(existingCategory);
                }
                else
                {
                    category.DateCreated = DateTime.UtcNow;
                    post.Categories.Add(category);
                }
            }

            var toRemove = new List<Category>();
            foreach(var category in post.Categories)
            {
                if(!categories.Any(c => c.Content == category.Content))
                {
                    toRemove.Add(category);
                }
            }

            foreach(var category in toRemove)
            {
                post.Categories.Remove(category);
            }

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveCategory(Guid categoryId)
        {
            var category = await _db.Categories
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            _db.Categories.Remove(category);

            return await _db.SaveChangesAsync() > 0;
        }
    }
}
