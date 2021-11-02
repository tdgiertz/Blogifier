using System;
using System.Collections.Generic;
using Blogifier.Shared;

namespace Blogifier.Core.Providers.MongoDb.Models
{
    public class PostAuthorAggregate
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

        public IEnumerable<Author> Author { get; set; }
        public Blog Blog { get; set; }
        public List<Category> Categories { get; set; }
    }
}
