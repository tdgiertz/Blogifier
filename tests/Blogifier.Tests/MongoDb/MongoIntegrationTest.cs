
using System;
using System.Collections.Generic;
using System.Threading;
using Blogifier.Core.Providers;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using Microsoft.Extensions.Configuration;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Blogifier.Tests.MongoDb
{
    public class MongoIntegrationTest : IDisposable
    {
        private MongoDbRunner _runner;
        protected readonly IMongoDatabase _database;
        protected readonly IMongoCollection<Post> _postCollection;
        protected readonly IMongoCollection<MongoBlog> _blogCollection;
        protected readonly IMongoCollection<Author> _authorCollection;
        protected readonly IConfiguration _configuration;

        protected Guid _category1Id;
        protected Guid _category2Id;
        protected Guid _category3Id;

        protected Guid _post1Id;
        protected Guid _post2Id;
        protected Guid _post3Id;
        protected Guid _post4Id;

        protected Guid _authorId;

        static MongoIntegrationTest()
        {
            BsonClassMappings.Register();
        }

        public MongoIntegrationTest()
        {
            _runner = MongoDbRunner.Start(singleNodeReplSet: true);

            var settings = MongoClientSettings.FromConnectionString(_runner.ConnectionString);

            settings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    Console.WriteLine(e.CommandName);
                    Console.WriteLine();
                    Console.WriteLine(e.Command.ToJson(new JsonWriterSettings { Indent = true }));
                    Console.WriteLine();
                });
            };

            var client = new MongoClient(settings);
            _database = client.GetDatabase("IntegrationTest");

            _postCollection = _database.GetNamedCollection<Post>();
            _authorCollection = _database.GetNamedCollection<Author>();
            _blogCollection = _database.GetNamedCollection<MongoBlog>();

            var inMemorySettings = new Dictionary<string, string> {
                {"Blogifier:Salt", "Test-Salt-Value"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            Seed();
        }

        private void Seed()
        {
            var category1 = new Category
            {
                Id = Guid.NewGuid(),
                Content = "Category 1 test",
                Description = "Category 1 Description",
                DateCreated = DateTime.Now
            };

            _category1Id = category1.Id;

            var category2 = new Category
            {
                Id = Guid.NewGuid(),
                Content = "Category 2",
                Description = "Category 2 Description",
                DateCreated = DateTime.Now
            };

            _category2Id = category2.Id;

            var category3 = new Category
            {
                Id = Guid.NewGuid(),
                Content = "Category 3",
                Description = "Category 3 Description",
                DateCreated = DateTime.Now
            };

            _category3Id = category3.Id;

            var blog = new MongoBlog
            {
                Id = Guid.NewGuid(),
                Title = "Test Blog",
            };

            _blogCollection.InsertOne(blog);

            var author = new Author
            {
                Id = Guid.NewGuid(),
                Email = "tester@test.com",
                Password = "np-pass",
                DisplayName = "Tester Person",
                BlogId = blog.Id
            };

            _authorId = author.Id;

            blog.Authors = new System.Collections.Generic.List<Author> { author };

            _authorCollection.InsertOne(author);

            var post1 = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Post 1",
                Slug = "Post 1 Slug",
                Description = "Post 1 Description",
                Content = "Post 1 Content",
                PostType = PostType.Post,
                Categories = new System.Collections.Generic.List<Category> { category1, category2, category3 },
                Blog = blog,
                BlogId = blog.Id,
                AuthorId = author.Id,
                Author = author
            };

            _post1Id = post1.Id;

            var post2 = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Post 2",
                Slug = "Post 12 Slug",
                Description = "Post 2 Description",
                Content = "Post 2 Content",
                PostType = PostType.Post,
                Categories = new System.Collections.Generic.List<Category> { category2 },
                Blog = blog,
                BlogId = blog.Id,
                AuthorId = author.Id,
                Author = author
            };

            _post2Id = post2.Id;

            var post3 = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Post 3 Test",
                Slug = "Post 3 Slug",
                Description = "Post 3 Description",
                Content = "Post 3 Content",
                PostType = PostType.Post,
                Categories = new System.Collections.Generic.List<Category> { category3 },
                Blog = blog,
                BlogId = blog.Id,
                AuthorId = author.Id,
                Author = author
            };

            _post3Id = post3.Id;

            var post4 = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Post 4",
                Slug = "Post 4 Slug",
                Description = "Post 4 Description",
                Content = "Post 4 Content",
                PostType = PostType.Post,
                Categories = new System.Collections.Generic.List<Category> { category2 },
                Blog = blog,
                BlogId = blog.Id,
                AuthorId = author.Id,
                Author = author
            };

            _post4Id = post4.Id;

            _postCollection.InsertMany(new[] { post1, post2, post3, post4 });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            if (_runner != null)
            {
                Thread.Sleep(500);
                try { _runner?.Dispose(); } catch { }
                _runner = null;
                GC.Collect();
            }
        }
    }
}
