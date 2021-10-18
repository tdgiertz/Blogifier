using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blogifier.Files.Extensions;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Iam.v1;
using Google.Apis.Services;
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
        private readonly UrlSigner? _urlSigner;

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
            if (configuration.UrlExpirationMinutes <= 0)
            {
                throw new System.ArgumentException("Argument property invalid", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.UrlExpirationMinutes)}");
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
            else
            {
                googleCredential = GoogleCredential.GetApplicationDefault();
            }

            if(!string.IsNullOrEmpty(configuration.AccountId))
            {
                var iamService = new IamService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = googleCredential
                });
                var blobSigner = new IamServiceBlobSigner(iamService, configuration.AccountId);
                _urlSigner = UrlSigner.FromBlobSigner(blobSigner);
            }

            _storageClient = StorageClient.Create(googleCredential);

            _configuration = configuration;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request)
        {
            if(_urlSigner == null)
            {
                throw new InvalidOperationException();
            }

            var objectPath = Path.Combine(_configuration.BasePath, request.Filename);

            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(request.Filename);

            var requestTemplate = UrlSigner.RequestTemplate
                .FromBucket(_configuration.StoreName)
                .WithHttpMethod(System.Net.Http.HttpMethod.Put)
                .WithObjectName(objectPath)
                .WithRequestHeaders(new Dictionary<string, IEnumerable<string>>
                {
                    { "Content-Type", new [] { mimeType } }
                });

            var options = UrlSigner.Options.FromDuration(TimeSpan.FromMinutes(_configuration.UrlExpirationMinutes));
            options.WithSigningVersion(SigningVersion.V4);

            var url = await _urlSigner.SignAsync(requestTemplate, options);

            return new SignedUrlResponse
            {
                Url = url,
                FileModel = new FileModel
                {
                    Filename = request.Filename,
                    Url = _configuration.ReplacePublicUrlTemplateValues(request.Filename, objectPath),
                    MimeType = mimeType,
                    RelativePath = objectPath,
                },
                Parameters = new Dictionary<string, string>
                {
                    { "Content-Type", mimeType }
                },
                DoesRequirePermissionUpdate = true
            };
        }

        public async Task SetObjectPublic(string objectName)
        {
            var objectPath = Path.Combine(_configuration.BasePath, objectName);
            var storageObject = await _storageClient.GetObjectAsync(_configuration.StoreName, objectPath);

            await _storageClient.UpdateObjectAsync(storageObject, new UpdateObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead });
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
