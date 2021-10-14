using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Blogifier.Files.Providers
{
    public interface IFileStoreProvider
    {
        Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request);
        Task<bool> ExistsAsync(string objectName);
        Task<FileResult> CreateAsync(IFormFile formFile);
        Task<bool> DeleteAsync(string objectName);
        IAsyncEnumerable<FileResult> ListAsync();
    }
}
