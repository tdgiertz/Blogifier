using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Blogifier.Shared
{
    public class Post
    {
        public Post()
        {
        }

        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public Guid BlogId { get; set; }
        public PostType PostType { get; set; }

        [Required]
        [StringLength(160)]
        public string Title { get; set; }
        [Required]
        [StringLength(160)]
        public string Slug { get; set; }
        [Required]
        [StringLength(450)]
        public string Description { get; set; }
        [Required]
        public string Content { get; set; }
        [StringLength(160)]
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
    }
}
