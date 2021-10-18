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
        [Required]
        public string RelativePath { get; set; }
        public string MimeType { get; set; }
        [Required]
        public string Url { get; set; }
        [StringLength(1000)]
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }

        public FileModel ToFileModel()
        {
            return new FileModel
            {
                Id = Id,
                Filename = Filename,
                RelativePath = RelativePath,
                MimeType = MimeType,
                Url = Url,
                Description = Description,
                DateCreated = DateCreated,
                DateUpdated = DateUpdated
            };
        }
    }
}
