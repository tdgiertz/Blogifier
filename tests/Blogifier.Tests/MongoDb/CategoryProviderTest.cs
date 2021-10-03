using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blogifier.Core.Providers.MongoDb;
using Blogifier.Shared;
using MongoDB.Driver;
using Xunit;

namespace Blogifier.Tests.MongoDb
{
    public class CategoryProviderTest : MongoIntegrationTest
    {
        [Fact]
        public async Task Can_Get_Categories()
        {
            var provider = new CategoryProvider(_database);

            var categories = (await provider.Categories()).OrderBy(c => c.Category).ToList();

            Assert.Equal(3, categories.Count);
            Assert.Equal("category 1 test", categories[0].Category);
            Assert.Equal("category 2", categories[1].Category);
            Assert.Equal("category 3", categories[2].Category);
            Assert.Equal(1, categories[0].PostCount);
            Assert.Equal(3, categories[1].PostCount);
            Assert.Equal(2, categories[2].PostCount);
        }

        [Fact]
        public async Task Can_Get_Category()
        {
            var provider = new CategoryProvider(_database);

            var category = await provider.GetCategory(_category1Id);

            Assert.Equal(_category1Id, category.Id);
        }

        [Fact]
        public async Task Can_Get_Post_Categories()
        {
            var provider = new CategoryProvider(_database);

            var postCategories = await provider.GetPostCategories(_post1Id);

            Assert.NotNull(postCategories.FirstOrDefault(c => c.Id == _category1Id));
            Assert.NotNull(postCategories.FirstOrDefault(c => c.Id == _category2Id));
            Assert.NotNull(postCategories.FirstOrDefault(c => c.Id == _category3Id));
        }

        [Fact]
        public async Task Can_Add_Post_Category()
        {
            var provider = new CategoryProvider(_database);

            var originalCategories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Content = "Original One", Description = "Original One" },
                new Category { Id = Guid.NewGuid(), Content = "Original Two", Description = "Original Two" },
                new Category { Id = Guid.NewGuid(), Content = "Original Three", Description = "Original Three" }
            };

            var newPost = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Nothing",
                Slug = "Nothing",
                Description = "Nothing",
                Content = "Nothing",
                Categories = originalCategories
            };

            _postCollection.InsertOne(newPost);

            var newCategory = "New Category";
            var hasAdded = await provider.AddPostCategory(newPost.Id, newCategory);

            Assert.True(hasAdded);

            var post = _postCollection.Find(p => p.Id == newPost.Id).FirstOrDefault();

            Assert.Equal(4, post.Categories.Count());
            Assert.NotNull(post.Categories.FirstOrDefault(c => c.Content == newCategory));

            _postCollection.DeleteOne(Builders<Post>.Filter.Eq(p => p.Id, newPost.Id));
        }

        [Fact]
        public async Task Can_Save_Post_Categories()
        {
            var provider = new CategoryProvider(_database);

            var originalCategories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Content = "Original One", Description = "Original One" },
                new Category { Id = Guid.NewGuid(), Content = "Original Two", Description = "Original Two" },
                new Category { Id = Guid.NewGuid(), Content = "Original Three", Description = "Original Three" }
            };

            var newPost = new Post
            {
                Title = "Nothing",
                Slug = "Nothing",
                Description = "Nothing",
                Content = "Nothing",
                Categories = originalCategories
            };

            _postCollection.InsertOne(newPost);

            var newCategories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Content = "New One", Description = "New One" },
                new Category { Id = Guid.NewGuid(), Content = "New Two", Description = "New Two" },
                new Category { Id = Guid.NewGuid(), Content = "New Three", Description = "New Three" }
            };

            var hasAdded = await provider.SavePostCategories(newPost.Id, newCategories);

            Assert.True(hasAdded);

            var post = _postCollection.Find(p => p.Id == newPost.Id).FirstOrDefault();

            Assert.Equal(newCategories.Count, post.Categories?.Count());
            foreach(var category in newCategories)
            {
                Assert.NotNull(post.Categories.FirstOrDefault(c => c.Content == category.Content));
            }

            _postCollection.DeleteOne(Builders<Post>.Filter.Eq(p => p.Id, newPost.Id));
        }

        [Fact]
        public async Task Can_Remove_Category()
        {
            var provider = new CategoryProvider(_database);

            var categories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Content = "One", Description = "One" },
                new Category { Id = Guid.NewGuid(), Content = "Two", Description = "Two" },
                new Category { Id = Guid.NewGuid(), Content = "Three", Description = "Three" }
            };

            var newPost1 = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Nothing",
                Slug = "Nothing",
                Description = "Nothing",
                Content = "Nothing",
                Categories = categories
            };

            var newPost2 = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Nothing",
                Slug = "Nothing",
                Description = "Nothing",
                Content = "Nothing",
                Categories = categories
            };

            _postCollection.InsertMany(new [] { newPost1, newPost2 });

            var post1 = _postCollection.Find(p => p.Id == newPost1.Id).FirstOrDefault();
            Assert.NotNull(post1);
            Assert.Equal(3, post1.Categories?.Count());

            var post2 = _postCollection.Find(p => p.Id == newPost2.Id).FirstOrDefault();
            Assert.NotNull(post2);
            Assert.Equal(3, post2.Categories?.Count());

            var hasDeleted = await provider.RemoveCategory(categories.First().Id);

            Assert.True(hasDeleted);

            post1 = _postCollection.Find(p => p.Id == newPost1.Id).FirstOrDefault();

            Assert.Equal(2, post1.Categories?.Count());
            Assert.Null(post1.Categories.FirstOrDefault(c => c.Id == categories.First().Id));

            post2 = _postCollection.Find(p => p.Id == newPost1.Id).FirstOrDefault();
            Assert.Equal(2, post2.Categories?.Count());
            Assert.Null(post2.Categories.FirstOrDefault(c => c.Id == categories.First().Id));

            _postCollection.DeleteOne(Builders<Post>.Filter.Eq(p => p.Id, post1.Id));
            _postCollection.DeleteOne(Builders<Post>.Filter.Eq(p => p.Id, post2.Id));
        }

        [Fact]
        public async Task Can_Get_Category_By_Id()
        {
            var provider = new CategoryProvider(_database);

            var category = await provider.GetCategory(_category1Id);

            Assert.Equal("Category 1 test", category.Content);
        }

        [Fact]
        public async Task Can_Get_Post_Category_By_Id()
        {
            var provider = new CategoryProvider(_database);

            var categories = (await provider.GetPostCategories(_post1Id)).OrderBy(c => c.Content).ToList();

            Assert.Equal(3, categories.Count);
            Assert.Equal("Category 1 test", categories[0].Content);
            Assert.Equal("Category 2", categories[1].Content);
            Assert.Equal("Category 3", categories[2].Content);
        }
    }
}
