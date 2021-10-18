using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Blogifier.Files.Extensions;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Blogifier.Shared.Models;
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
            if(configuration.UrlExpirationMinutes <= 0)
            {
                throw new System.ArgumentException("Argument property invalid", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.UrlExpirationMinutes)}");
            }
            if(string.IsNullOrEmpty(configuration.ThumbnailBasePath))
            {
                configuration.ThumbnailBasePath = Path.Combine(configuration.BasePath, "Thumbnails");
            }

            var blobServiceClient = new BlobServiceClient(configuration.AuthenticationKey);
            _containerClient = blobServiceClient.GetBlobContainerClient(configuration.StoreName);

            _configuration = configuration;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request)
        {
            var clockSkew = TimeSpan.FromSeconds(5);
            var objectPath = Path.Combine(_configuration.BasePath, request.Filename);
            var blobClient = _containerClient.GetBlobClient(objectPath);

            var blobSasBuilder = new BlobSasBuilder
            {
                StartsOn = DateTime.UtcNow.Subtract(clockSkew),
                ExpiresOn = DateTime.UtcNow.Add(TimeSpan.FromMinutes(_configuration.UrlExpirationMinutes)) + clockSkew,
                BlobContainerName = _configuration.StoreName,
                BlobName = blobClient.Name
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var uri = blobClient.GenerateSasUri(blobSasBuilder);

            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(request.Filename);

            var response = new SignedUrlResponse
            {
                Url = uri.ToString(),
                FileModel = new FileModel
                {
                    Filename = request.Filename,
                    Url = _configuration.ReplacePublicUrlTemplateValues(request.Filename, objectPath, _containerClient.AccountName),
                    MimeType = mimeType,
                    RelativePath = objectPath,
                },
                Parameters = new Dictionary<string, string>
                {
                    { "x-ms-blob-type", "BlockBlob" },
                    { "Content-Type", mimeType }
                },
                DoesRequirePermissionUpdate = false
            };

            return await Task.FromResult(response);
        }

        public async Task SetObjectPublic(string objectName)
        {
            await Task.CompletedTask;
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

            BlobContentInfo result;
            if(await ExistsAsync(filename))
            {
                var blobClient = _containerClient.GetBlobClient(objectPath);
                result = await blobClient.UploadAsync(formFile.OpenReadStream(), true);
            }
            else
            {
                result = await _containerClient.UploadBlobAsync(objectPath, formFile.OpenReadStream());
            }

            return new FileResult
            {
                Url = _configuration.ReplacePublicUrlTemplateValues(filename, objectPath, _containerClient.AccountName),
                Path = objectPath,
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
                    Url = _configuration.ReplacePublicUrlTemplateValues(filename, item.Blob.Name, _containerClient.AccountName),
                    Filename = filename,
                    Path = item.Blob.Name,
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(filename)
                };
            }
        }
    }
}
