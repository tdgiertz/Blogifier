using System;

namespace Blogifier.Shared
{
    public class FileModel
    {
        public Guid Id { get; set; }
        public string Filename { get; set; }
        public string RelativePath { get; set; }
        public string MimeType { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ThumbnailRelativePath { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }

        public bool HasErrors { get; set; }
        public string Message { get; set; }

        public FileDescriptor ToFileDescriptor()
        {
            return new FileDescriptor
            {
                Id = Id,
                Filename = Filename,
                RelativePath = RelativePath,
                MimeType = MimeType,
                ThumbnailRelativePath = ThumbnailRelativePath,
                Description = Description,
                DateCreated = DateCreated,
                DateUpdated = DateUpdated
            };
        }
    }
}
