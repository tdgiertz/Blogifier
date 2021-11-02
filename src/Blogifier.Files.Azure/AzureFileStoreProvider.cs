using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using MimeMapping;
using Serilog;

namespace Blogifier.Files.Azure
{
    public class AzureFileStoreProvider : IFileStoreProvider
    {
        private readonly string _accountName;
        private readonly FileStoreConfiguration _configuration;
        private readonly BlobContainerClient _containerClient;

        public AzureFileStoreProvider(FileStoreConfiguration configuration)
        {
            if(string.IsNullOrEmpty(configuration.StoreName))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.StoreName)}");
            }
            if(configuration.UrlExpirationMinutes <= 0)
            {
                throw new System.ArgumentException("Argument property invalid", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.UrlExpirationMinutes)}");
            }

            var blobServiceClient = new BlobServiceClient(configuration.AuthenticationKey);
            _containerClient = blobServiceClient.GetBlobContainerClient(configuration.StoreName);

            _configuration = configuration;
            _accountName = _containerClient.AccountName;
        }

        public string GetAccountName()
        {
            return _accountName;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(GenerateSignedUrl generateSignedUrl)
        {
            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(generateSignedUrl.Filename);

            var url = GenerateSignedUrl(generateSignedUrl.FilePath);
            string? thumbnailUrl = null;

            if(!string.IsNullOrEmpty(generateSignedUrl.ThumbnailFilePath))
            {
                thumbnailUrl = GenerateSignedUrl(generateSignedUrl.ThumbnailFilePath);
            }

            var response = new SignedUrlResponse
            {
                Url = url,
                ThumbnailUrl = thumbnailUrl,
                FileModel = new FileModel
                {
                    Filename = generateSignedUrl.Filename,
                    MimeType = mimeType,
                    RelativePath = generateSignedUrl.FilePath,
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

        private string GenerateSignedUrl(string filePath)
        {
            var clockSkew = TimeSpan.FromSeconds(5);
            var blobClient = _containerClient.GetBlobClient(filePath);

            var blobSasBuilder = new BlobSasBuilder
            {
                StartsOn = DateTime.UtcNow.Subtract(clockSkew),
                ExpiresOn = DateTime.UtcNow.Add(TimeSpan.FromMinutes(_configuration.UrlExpirationMinutes)) + clockSkew,
                BlobContainerName = _configuration.StoreName,
                BlobName = blobClient.Name
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var uri = blobClient.GenerateSasUri(blobSasBuilder);

            return uri.ToString();
        }

        public async Task SetObjectPublic(string objectPath)
        {
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string objectPath)
        {
            return await _containerClient.GetBlobClient(objectPath).ExistsAsync();
        }

        public async Task<FileResult> CreateAsync(string objectPath, Stream stream)
        {
            var filename = Path.GetFileName(objectPath);
            var mimeType = MimeUtility.GetMimeMapping(filename);

            BlobContentInfo result;
            if(await ExistsAsync(filename))
            {
                var blobClient = _containerClient.GetBlobClient(objectPath);
                result = await blobClient.UploadAsync(stream, true);
            }
            else
            {
                result = await _containerClient.UploadBlobAsync(objectPath, stream);
            }

            return new FileResult
            {
                Path = objectPath,
                Filename = filename,
                MimeType = mimeType,
            };
        }

        public async Task<bool> DeleteAsync(string objectPath)
        {
            try
            {
                var result = await _containerClient.DeleteBlobAsync(objectPath);
                return true;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Azure DeleteObjectAsync failed");
                return false;
            }
        }

        public async IAsyncEnumerable<FileResult> ListAsync(string objectPath)
        {
            var containerUri = _containerClient.Uri;
            await foreach (var item in _containerClient.GetBlobsByHierarchyAsync(delimiter: "/"))
            {
                if(!item.IsBlob) continue;
                var filename = Path.GetFileName(item.Blob.Name);
                yield return new FileResult
                {
                    Filename = filename,
                    Path = item.Blob.Name,
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(filename)
                };
            }
        }
    }
}
