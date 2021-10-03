using Blogifier.Core.Data;
using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;
using System;

namespace Blogifier.Tests.EfCore
{
    public class ProviderTest
    {
        protected Guid _category1Id;
        protected Guid _category2Id;
        protected Guid _category3Id;

        protected Guid _post1Id;
        protected Guid _post2Id;
        protected Guid _post3Id;
        protected Guid _post4Id;

        protected ProviderTest(DbContextOptions<AppDbContext> contextOptions)
        {
            ContextOptions = contextOptions;

            Seed();
        }

        protected DbContextOptions<AppDbContext> ContextOptions { get; }

        private void Seed()
        {
            using (var context = new AppDbContext(ContextOptions))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                var category1 = new Category
                {
                    Id = Guid.NewGuid(),
                    Content = "Category 1",
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

                context.Categories.AddRange(category1, category2, category3);

                context.SaveChanges();

                var blog = new Blog
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Blog",
                };

                var author = new Author
                {
                    Id = Guid.NewGuid(),
                    Email = "tester@test.com",
                    Password = "np-pass",
                    DisplayName = "Tester Person",
                    BlogId = blog.Id
                };

                blog.Authors = new System.Collections.Generic.List<Author> { author };

                context.Authors.Add(author);

                context.Blogs.Add(blog);

                context.SaveChanges();

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
                    AuthorId = author.Id
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
                    AuthorId = author.Id
                };

                _post2Id = post2.Id;

                var post3 = new Post
                {
                    Id = Guid.NewGuid(),
                    Title = "Post 3",
                    Slug = "Post 3 Slug",
                    Description = "Post 3 Description",
                    Content = "Post 3 Content",
                    PostType = PostType.Post,
                    Categories = new System.Collections.Generic.List<Category> { category3 },
                    Blog = blog,
                    AuthorId = author.Id
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
                    AuthorId = author.Id
                };

                _post4Id = post4.Id;

                context.Posts.AddRange(post1, post2, post3, post4);

                context.SaveChanges();
            }
        }
    }
}
