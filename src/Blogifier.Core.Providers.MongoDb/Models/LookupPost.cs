using System;
using System.Collections.Generic;
using Blogifier.Shared;

namespace Blogifier.Core.Providers.MongoDb.Models
{
    public class LookupPost
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public Guid BlogId { get; set; }
        public PostType PostType { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Cover { get; set; }
        public int PostViews { get; set; }
        public double Rating { get; set; }
        public bool IsFeatured { get; set; }
        public bool Selected { get; set; }

        public DateTime Published { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }

        public Author Author { get; set; }
        public Blog Blog { get; set; }
        public List<Category> Categories { get; set; }

        public Post ToPost()
        {
            return new Post
            {
                Id = this.Id,
                AuthorId = this.AuthorId,
                BlogId = this.BlogId,
                PostType = this.PostType,
                Title = this.Title,
                Slug = this.Slug,
                Description = this.Description,
                Content = this.Content,
                Cover = this.Cover,
                PostViews = this.PostViews,
                Rating = this.Rating,
                IsFeatured = this.IsFeatured,
                Selected = this.Selected,
                Published = this.Published,
                DateCreated = this.DateCreated,
                DateUpdated = this.DateUpdated,
                Categories = this.Categories,
                Author = this.Author
            };
        }
    }
}
