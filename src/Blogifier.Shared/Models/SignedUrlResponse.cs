using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Blogifier.Shared.Models
{
    public class SignedUrlResponse
    {
        [NotNull]
        public string Url { get; set; }
        [NotNull]
        public IDictionary<string, string> Parameters { get; set; }
    }
}
