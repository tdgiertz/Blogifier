using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Blogifier.Shared.Models
{
    public class SignedUrlResponse
    {
        [NotNull]
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        [NotNull]
        public IDictionary<string, string> Parameters { get; set; }
        [NotNull]
        public FileModel FileModel { get; set; }
        [NotNull]
        public bool DoesRequirePermissionUpdate { get; set; }

        public FileDescriptor ToFileDescriptor()
        {
            return FileModel.ToFileDescriptor();
        }
    }
}
