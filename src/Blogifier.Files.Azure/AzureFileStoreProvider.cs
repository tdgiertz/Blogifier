using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
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

            var blobSasBuilder = new BlobSasBuilder
            {
                StartsOn = DateTime.UtcNow.Subtract(clockSkew),
                ExpiresOn = DateTime.UtcNow.Add(TimeSpan.FromMinutes(_configuration.UrlExpirationMinutes)) + clockSkew,
                BlobContainerName = _configuration.StoreName,
                BlobName = objectPath
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var connectionBuilder = new System.Data.Common.DbConnectionStringBuilder();
            connectionBuilder.ConnectionString = _configuration.AuthenticationKey;
            var ssk = new StorageSharedKeyCredential(_containerClient.AccountName, connectionBuilder["AccountKey"] as string);
            var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(ssk).ToString();

            var uri = new UriBuilder()
            {
                Scheme = "https",
                Host = string.Format("{0}.blob.core.windows.net", _containerClient.AccountName),
                Path = string.Format("{0}/{1}", _configuration.StoreName, objectPath),
                Query = sasQueryParameters
            };

            var response = new SignedUrlResponse
            {
                Url = uri.ToString(),
                Parameters = new Dictionary<string, string>
                {
                    { "x-ms-blob-type", "BlockBlob" }
                }
            };

            return await Task.FromResult(response);
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
