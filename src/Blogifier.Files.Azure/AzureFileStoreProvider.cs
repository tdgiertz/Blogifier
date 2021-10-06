using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Microsoft.AspNetCore.Http;
using MimeMapping;
using Serilog;

namespace Blogifier.Files.Azure
{
    public class AzureFileStoreProvider : IFileStoreProvider
    {
        private readonly FileStoreConfiguration _configuration;
        private readonly BlobContainerClient _containerClient;

        public AzureFileStoreProvider(FileStoreConfiguration configuration)
        {
            if(string.IsNullOrEmpty(configuration.BasePath))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.BasePath)}");
            }
            if(string.IsNullOrEmpty(configuration.StoreName))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.StoreName)}");
            }
            if(string.IsNullOrEmpty(configuration.ThumbnailBasePath))
            {
                configuration.ThumbnailBasePath = Path.Combine(configuration.BasePath, "Thumbnails");
            }

            var blobServiceClient = new BlobServiceClient(configuration.AuthenticationKey);
            _containerClient = blobServiceClient.GetBlobContainerClient(configuration.StoreName);

            _configuration = configuration;
        }

        public async Task<bool> ExistsAsync(string objectName)
        {
            return await _containerClient.GetBlobClient(objectName).ExistsAsync();
        }

        public async Task<FileResult> CreateAsync(IFormFile formFile)
        {
            var filename = Path.GetFileName(formFile.FileName);
            var mimeType = MimeUtility.GetMimeMapping(filename);

            using var stream = new MemoryStream();
            await formFile.OpenReadStream().CopyToAsync(stream);

            var result = await _containerClient.UploadBlobAsync(filename, stream);
            var uri = _containerClient.GetBlobClient(filename).Uri;

            return new FileResult
            {
                Url = uri.AbsolutePath,
                Filename = filename,
                MimeType = mimeType,
            };
        }

        public async Task<bool> DeleteAsync(string objectName)
        {
            try
            {
                var result = await _containerClient.DeleteBlobAsync(objectName);
                return true;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Azure DeleteObjectAsync failed");
                return false;
            }
        }

        public async IAsyncEnumerable<FileResult> ListAsync()
        {
            var containerUri = _containerClient.Uri;
            await foreach (var item in _containerClient.GetBlobsAsync())
            {
                var filename = Path.GetFileName(item.Name);
                yield return new FileResult
                {
                    Url = Flurl.Url.Combine(_containerClient.Uri.AbsoluteUri, item.Name),
                    Filename = filename,
                    Path = item.Name,
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(filename)
                };
            }
        }
    }
}
