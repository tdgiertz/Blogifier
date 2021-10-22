using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blogifier.Files.Models;
using Blogifier.Shared;
using Blogifier.Shared.Models;

namespace Blogifier.Files.Providers
{
    public class FileStoreProvider : IFileStoreProvider
    {
        private readonly FileStoreConfiguration _fileStoreConfiguration;
        public FileStoreProvider(FileStoreConfiguration fileStoreConfiguration)
        {
            _fileStoreConfiguration = fileStoreConfiguration;
        }

        public string GetAccountName()
        {
            throw new System.NotImplementedException();
        }

        public Task<SignedUrlResponse> GetSignedUrlAsync(GenerateSignedUrl generateSignedUrl)
        {
            throw new System.NotImplementedException();
        }

        public Task SetObjectPublic(string objectName)
        {
            throw new System.NotImplementedException();
        }

        public Task<FileResult> CreateAsync(string objectPath, Stream stream)
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
