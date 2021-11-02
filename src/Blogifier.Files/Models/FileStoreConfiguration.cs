using Blogifier.Files.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Blogifier.Files.Models
{
    public class FileStoreConfiguration
    {
        private string? _basePath;
        private string? _thumbnailBasePath;

        public string? AuthenticationKeyId { get; set; }
        public string? AuthenticationKey { get; set; }
        [NotNull]
        public string? Endpoint { get; set; }
        public KeySource AuthenticationKeySource { get; set; }
        [NotNull]
        public string? StoreName { get; set; }
        [NotNull]
        public string? BasePathTemplate { get; set; }
        [NotNull]
        public string? ThumbnailBasePathTemplate { get; set; }
        [NotNull]
        public string? PublicUrlTemplate { get; set; }
        [NotNull]
        public int UrlExpirationMinutes { get; set; }
        public string? AccountId { get; set; }

        public async Task<string> GetBasePathAsync(Func<Task<Guid>> getAuthorId)
        {
            if(string.IsNullOrEmpty(BasePathTemplate)) return BasePathTemplate ?? string.Empty;

            if(_basePath != null) return _basePath;

            _basePath = await BasePathTemplate.ReplaceTemplateValuesAsync(null, null, this, getAuthorId);

            return _basePath;
        }

        public async Task<string> GetThumbnailBasePathAsync(Func<Task<Guid>> getAuthorId)
        {
            if(string.IsNullOrEmpty(ThumbnailBasePathTemplate)) return ThumbnailBasePathTemplate ?? string.Empty;

            if(_thumbnailBasePath != null) return _thumbnailBasePath;

            _thumbnailBasePath = await ThumbnailBasePathTemplate.ReplaceTemplateValuesAsync(null, null, this, getAuthorId);

            return _thumbnailBasePath;
        }
    }
}
