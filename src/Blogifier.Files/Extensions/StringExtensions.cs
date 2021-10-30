using System;
using System.Threading.Tasks;
using Blogifier.Files.Models;

namespace Blogifier.Files.Extensions
{
    public static class StringExtensions
    {
        public async static Task<string> ReplacePublicUrlTemplateValuesAsync(this FileStoreConfiguration configuration, string filename, string filepath, Func<Task<Guid>> getAuthorId, string? accountName = null)
        {
            return await configuration.PublicUrlTemplate.ReplaceTemplateValuesAsync(filename, filepath, configuration, getAuthorId);
        }

        public async static Task<string> ReplaceTemplateValuesAsync(this string value, string? filename, string? filepath, FileStoreConfiguration configuration, Func<Task<Guid>> getAuthorId, string? accountName = null)
        {
            var dateTime = DateTime.Now;
            var result = value.Replace("{StoreName}", configuration.StoreName, StringComparison.InvariantCultureIgnoreCase);
            if(!string.IsNullOrEmpty(filepath))
            {
                result = result.Replace("{FilePath}", filepath, StringComparison.InvariantCultureIgnoreCase);
            }
            if(!string.IsNullOrEmpty(filename))
            {
                result = result.Replace("{FileName}", filename, StringComparison.InvariantCultureIgnoreCase);
            }
            result = result.Replace("{Region}", configuration.Endpoint, StringComparison.InvariantCultureIgnoreCase);
            result = result.Replace("{Year}", $"{dateTime.Year}", StringComparison.CurrentCultureIgnoreCase);
            result = result.Replace("{Month}", $"{dateTime.Month}", StringComparison.CurrentCultureIgnoreCase);
            result = result.Replace("{Day}", $"{dateTime.Day}", StringComparison.CurrentCultureIgnoreCase);
            if(accountName != null)
            {
                result = result.Replace("{AccountName}", accountName, StringComparison.InvariantCultureIgnoreCase);
            }
            if(result.Contains("{BasePath}"))
            {
                result = result.Replace("{BasePath}", await configuration.GetBasePathAsync(getAuthorId), StringComparison.InvariantCultureIgnoreCase);
            }
            if(result.Contains("{AuthorId}", StringComparison.InvariantCultureIgnoreCase))
            {
                var authorId = (await getAuthorId()).ToString("N");
                result = result.Replace("{AuthorId}", $"{authorId}", StringComparison.CurrentCultureIgnoreCase);
            }

            return result;
        }
    }
}
