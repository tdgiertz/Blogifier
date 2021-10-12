using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blogifier.Files.Extensions;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using MimeMapping;
using Serilog;

namespace Blogifier.Files.Google
{
    public class GoogleFileStoreProvider : IFileStoreProvider
    {
        private readonly FileStoreConfiguration _configuration;
        private readonly StorageClient _storageClient;

        public GoogleFileStoreProvider(FileStoreConfiguration configuration)
        {
            configuration.BasePath ??= "";
            if (string.IsNullOrEmpty(configuration.StoreName))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.StoreName)}");
            }
            if (string.IsNullOrEmpty(configuration.PublicUrlTemplate))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.PublicUrlTemplate)}");
            }
            if (string.IsNullOrEmpty(configuration.ThumbnailBasePath))
            {
                configuration.ThumbnailBasePath = Path.Combine(configuration.BasePath, "Thumbnails");
            }

            GoogleCredential? googleCredential = null;

            if (configuration.AuthenticationKeySource == KeySource.File || configuration.AuthenticationKeySource == KeySource.String)
            {
                if (string.IsNullOrEmpty(configuration.AuthenticationKey))
                {
                    throw new System.ArgumentException($"Parameter value is required based on current value of {nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.AuthenticationKeySource)}",
                        $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.AuthenticationKey)}");
                }

                googleCredential = configuration.AuthenticationKeySource == KeySource.File
                    ? GoogleCredential.FromFile(configuration.AuthenticationKey)
                    : GoogleCredential.FromJson(configuration.AuthenticationKey);
            }

            _storageClient = StorageClient.Create(googleCredential);

            _configuration = configuration;
        }

        public async Task<bool> ExistsAsync(string objectName)
        {
            try
            {
                var objectPath = Path.Combine(_configuration.BasePath, objectName);
                await _storageClient.GetObjectAsync(_configuration.StoreName, objectPath);

                return false;
            }
            catch (GoogleApiException ex)
            {
                if (ex.Error.Code == 404)
                {
                    return true;
                }

                throw;
            }
        }

        public async Task<FileResult> CreateAsync(IFormFile formFile)
        {
            var filename = Path.GetFileName(formFile.FileName);
            var objectPath = Path.Combine(_configuration.BasePath, filename);
            var mimeType = MimeUtility.GetMimeMapping(filename);

            var uploadOptions = new UploadObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.PublicRead
            };

            var result = await _storageClient.UploadObjectAsync(_configuration.StoreName, objectPath, mimeType, formFile.OpenReadStream(), uploadOptions);

            return new FileResult
            {
                Url = _configuration.ReplacePublicUrlTemplateValues(filename, result.Name),
                Filename = filename,
                Path = result.Name,
                MimeType = mimeType
            };
        }

        public async Task<bool> DeleteAsync(string objectName)
        {
            try
            {
                var objectPath = Path.Combine(_configuration.BasePath, objectName);
                await _storageClient.DeleteObjectAsync(_configuration.StoreName, objectPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Google DeleteObjectAsync failed");
                return false;
            }
        }

        public async IAsyncEnumerable<FileResult> ListAsync()
        {
            var onlyCurrentDirectoryOptions = new ListObjectsOptions
            {
                Delimiter = "/"
            };
            await foreach (var item in _storageClient.ListObjectsAsync(_configuration.StoreName, options: onlyCurrentDirectoryOptions))
            {
                var filename = Path.GetFileName(item.Name);
                yield return new FileResult
                {
                    Url = _configuration.ReplacePublicUrlTemplateValues(filename, item.Name),
                    Filename = filename,
                    Path = item.Name,
                    MimeType = item.ContentType
                };
            }
        }
    }
}
