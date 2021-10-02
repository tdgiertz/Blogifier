using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Blogifier.Shared
{
    public class Category
    {
        public Category()
        {
        }

        public Guid Id { get; set; }
        [Required]
        [StringLength(120)]
        public string Content { get; set; }
        [StringLength(255)]
        public string Description { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }

        public List<Post> Posts { get; set; }
    }
}
