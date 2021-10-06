using System;

namespace Blogifier.Shared
{
    public class FileModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public string Uri { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
