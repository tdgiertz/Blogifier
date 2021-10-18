using System;
using Blogifier.Files.Models;

namespace Blogifier.Files.Extensions
{
    public static class StringExtensions
    {
        public static string ReplacePublicUrlTemplateValues(this FileStoreConfiguration configuration, string filename, string filepath, string? accountName = null)
        {
            return configuration.PublicUrlTemplate.ReplaceTemplateValues(filename, filepath, configuration);
        }

        public static string ReplaceTemplateValues(this string value, string filename, string filepath, FileStoreConfiguration configuration, string? accountName = null)
        {
            var result = value.Replace("{StoreName}", configuration.StoreName, StringComparison.InvariantCultureIgnoreCase);
            result = result.Replace("{FilePath}", filepath, StringComparison.InvariantCultureIgnoreCase);
            result = result.Replace("{FileName}", filename, StringComparison.InvariantCultureIgnoreCase);
            result = result.Replace("{Region}", configuration.Endpoint, StringComparison.InvariantCultureIgnoreCase);
            result = result.Replace("{BasePath}", configuration.BasePath, StringComparison.InvariantCultureIgnoreCase);
            if(accountName != null)
            {
                result = result.Replace("{AccountName}", accountName, StringComparison.InvariantCultureIgnoreCase);
            }

            return result;
        }
    }
}
