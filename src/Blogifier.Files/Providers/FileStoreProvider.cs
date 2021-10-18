using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Files.Models;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Blogifier.Files.Providers
{
    public class FileStoreProvider : IFileStoreProvider
    {
        private readonly FileStoreConfiguration _fileStoreConfiguration;
        public FileStoreProvider(FileStoreConfiguration fileStoreConfiguration)
        {
            _fileStoreConfiguration = fileStoreConfiguration;
        }

        public Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task SetObjectPublic(string objectName)
        {
            throw new System.NotImplementedException();
        }

        public Task<FileResult> CreateAsync(IFormFile formFile)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> DeleteAsync(string objectName)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> ExistsAsync(string objectName)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<FileResult> ListAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
