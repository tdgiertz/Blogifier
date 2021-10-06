using System;
using System.ComponentModel.DataAnnotations;

namespace Blogifier.Shared
{
    public class FileDescriptor
    {
        public FileDescriptor()
        {
        }

        public Guid Id { get; set; }
        [Required]
        public string Filename { get; set; }
        public string MimeType { get; set; }
        [Required]
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
