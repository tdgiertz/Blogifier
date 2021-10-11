using System;

namespace Blogifier.Shared
{
    public class FileModel
    {
        public Guid Id { get; set; }
        public string Filename { get; set; }
        public string MimeType { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }

        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
    }
}
