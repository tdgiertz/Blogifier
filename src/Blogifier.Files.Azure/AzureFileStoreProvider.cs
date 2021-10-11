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
            configuration.BasePath ??= "";
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
            var objectPath = Path.Combine(_configuration.BasePath, objectName);
            return await _containerClient.GetBlobClient(objectPath).ExistsAsync();
        }

        public async Task<FileResult> CreateAsync(IFormFile formFile)
        {
            var filename = Path.GetFileName(formFile.FileName);
            var objectPath = Path.Combine(_configuration.BasePath, filename);
            var mimeType = MimeUtility.GetMimeMapping(filename);

            var result = await _containerClient.UploadBlobAsync(objectPath, formFile.OpenReadStream());
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
                var objectPath = Path.Combine(_configuration.BasePath, objectName);
                var result = await _containerClient.DeleteBlobAsync(objectPath);
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
            await foreach (var item in _containerClient.GetBlobsByHierarchyAsync(delimiter: "/"))
            {
                if(!item.IsBlob) continue;
                var filename = Path.GetFileName(item.Blob.Name);
                yield return new FileResult
                {
                    Url = Flurl.Url.Combine(_containerClient.Uri.AbsoluteUri, item.Blob.Name),
                    Filename = filename,
                    Path = item.Blob.Name,
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(filename)
                };
            }
        }
    }
}
