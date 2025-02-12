using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    public class CategoryProvider : ICategoryProvider
    {
        private readonly IMongoCollection<Post> _postCollection;

        public CategoryProvider(IMongoDatabase db)
        {
            _postCollection = db.GetNamedCollection<Post>();
        }

        public async Task<List<CategoryItem>> Categories()
        {
            return await GetCategoryItemsAsync();
        }

        public async Task<List<CategoryItem>> GetPublishedCategories()
        {
            return await GetCategoryItemsAsync(c => c.Published != DateTime.MinValue);
        }

        private async Task<List<CategoryItem>> GetCategoryItemsAsync(Expression<Func<Post, bool>> predicate = null)
        {
            var aggregate = _postCollection.Aggregate();

            if(predicate != null)
            {
                aggregate = aggregate.Match(predicate);
            }

            var postCategories = await aggregate
                .Unwind<Post, CategoryUnwound>(p => p.Categories)
                .Group(c => c.Categories.Content, g => new
                {
                    Content = g.Key,
                    Id = g.First().Categories.Id,
                    PostCount = g.Count(),
                    DateCreated = g.First().Categories.DateCreated
                })
                .ToListAsync();

            var categories = postCategories.Select(c => new CategoryItem
            {
                Selected = false,
                Id = c.Id,
                Category = c.Content.ToLower(),
                PostCount = c.PostCount,
                DateCreated = c.DateCreated
            })
            .OrderBy(c => c.Category)
            .ToList();

            return categories;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            var categories = await _postCollection
                .Aggregate()
                .Unwind<Post, CategoryUnwound>(p => p.Categories)
                .Group(c => c.Categories.Content, g => new Category
                {
                    Id = g.First().Categories.Id,
                    Content = g.Key,
                    Description = g.First().Categories.Description,
                    DateCreated = g.First().Categories.DateCreated,
                    DateUpdated = g.First().Categories.DateUpdated,
                })
                .ToListAsync();

            return categories;
        }

        public async Task<List<CategoryItem>> SearchCategories(string term)
        {
            var categories = await Categories();

            if (term == "*")
                return categories;

            return categories.Where(c => c.Category.ToLower().Contains(term.ToLower())).ToList();
        }

        public async Task<Category> GetCategory(Guid categoryId)
        {
            var filter = Builders<Post>.Filter.ElemMatch(p => p.Categories, c => c.Id == categoryId);

            var post = await _postCollection.Find(filter).FirstOrDefaultAsync();

            return post.Categories.FirstOrDefault(c => c.Id.Equals(categoryId));
        }

        public async Task<ICollection<Category>> GetPostCategories(Guid postId)
        {
            var post = await _postCollection
                .Find(p => p.Id == postId)
                .FirstOrDefaultAsync();

            return post?.Categories ?? new List<Category>();
        }

        public async Task<bool> SaveCategory(Category category)
        {
            var filter = Builders<Post>.Filter.ElemMatch(p => p.Categories, c => c.Id == category.Id);

            var updateDefinition = Builders<Post>.Update
                .Set(p => p.Categories[-1].Content, category.Content)
                .Set(p => p.Categories[-1].Description, category.Description)
                .Set(p => p.Categories[-1].DateUpdated, category.DateUpdated);

            var result = await _postCollection.UpdateManyAsync(filter, updateDefinition);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public Task<Category> SaveCategory(string tag)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AddPostCategory(Guid postId, string tag)
        {
            var post = await _postCollection.Find(p => p.Id == postId).FirstOrDefaultAsync();
            if (post == null)
            {
                return false;
            }

            var category = new Category()
            {
                Id = Guid.NewGuid(),
                Content = tag,
                DateCreated = DateTime.UtcNow
            };

            post.Categories ??= new List<Category>();

            if(post.Categories.Any(c => c.Content.Equals(tag, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            post.Categories.Add(category);

            var result = await _postCollection.ReplaceOneAsync(p => p.Id == postId, post);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> SavePostCategories(Guid postId, List<Category> categories)
        {
            var updateDefinition = Builders<Post>.Update
                .Set(p => p.Categories, categories);

            var result = await _postCollection.UpdateOneAsync(p => p.Id == postId, updateDefinition);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveCategory(Guid categoryId)
        {
            var filter = Builders<Post>.Filter.ElemMatch(p => p.Categories, c => c.Id == categoryId);
            var updateDefinition = Builders<Post>.Update.PullFilter(p => p.Categories, c => c.Id == categoryId);

            var result = await _postCollection.UpdateManyAsync(filter, updateDefinition);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
    }
}
