using System;

namespace Blogifier.Shared
{
    public class FileResult
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public string MimeType { get; set; }

        public FileDescriptor ToFileDescriptor(string thumbnailRelativePath)
        {
            return new FileDescriptor
            {
                Id = Guid.NewGuid(),
                Filename = Filename,
                RelativePath = Path,
                MimeType = MimeType,
                ThumbnailRelativePath = thumbnailRelativePath,
                DateCreated = DateTime.UtcNow
            };
        }
    }
}
