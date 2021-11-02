using System.Diagnostics.CodeAnalysis;

namespace Blogifier.Shared.Models
{
    public class GenerateSignedUrl
    {
        [NotNull]
        public string Filename { get; set; }
        [NotNull]
        public string FilePath { get; set; }
        [NotNull]
        public string ThumbnailFilePath { get; set; }
    }
}
