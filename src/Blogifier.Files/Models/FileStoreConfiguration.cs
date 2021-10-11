using System.Diagnostics.CodeAnalysis;

namespace Blogifier.Files.Models
{
    public class FileStoreConfiguration
    {
        [NotNull]
        public string? AuthenticationKey { get; set; }
        public KeySource AuthenticationKeySource { get; set; }
        [NotNull]
        public string? StoreName { get; set; }
        [NotNull]
        public string? BasePath {get;set;}
        public string? ThumbnailBasePath {get;set;}
    }
}
