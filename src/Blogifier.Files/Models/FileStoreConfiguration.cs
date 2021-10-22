using System.Diagnostics.CodeAnalysis;

namespace Blogifier.Files.Models
{
    public class FileStoreConfiguration
    {
        public string? AuthenticationKeyId { get; set; }
        public string? AuthenticationKey { get; set; }
        [NotNull]
        public string? Endpoint { get; set; }
        public KeySource AuthenticationKeySource { get; set; }
        [NotNull]
        public string? StoreName { get; set; }
        [NotNull]
        public string? BasePath { get; set; }
        [NotNull]
        public string? ThumbnailBasePath { get; set; }
        [NotNull]
        public string? PublicUrlTemplate { get; set; }
        [NotNull]
        public int UrlExpirationMinutes { get; set; }
        public string? AccountId { get; set; }
    }
}
