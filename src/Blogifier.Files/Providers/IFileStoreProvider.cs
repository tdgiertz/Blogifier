using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blogifier.Shared;
using Blogifier.Shared.Models;

namespace Blogifier.Files.Providers
{
    public interface IFileStoreProvider
    {
        string GetAccountName();
        Task<SignedUrlResponse> GetSignedUrlAsync(GenerateSignedUrl generateSignedUrl);
        Task SetObjectPublic(string objectPath);
        Task<bool> ExistsAsync(string objectPath);
        Task<FileResult> CreateAsync(string objectPath, Stream stream);
        Task<bool> DeleteAsync(string objectPath);
        IAsyncEnumerable<FileResult> ListAsync();
    }
}
