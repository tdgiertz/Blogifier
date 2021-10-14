using System.Diagnostics.CodeAnalysis;

namespace Blogifier.Shared.Models
{
    public class SignedUrlRequest
    {
        [NotNull]
        public string Filename { get; set; }
    }
}
