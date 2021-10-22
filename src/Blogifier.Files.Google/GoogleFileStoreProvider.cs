using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Iam.v1;
using Google.Apis.Services;
using Google.Cloud.Storage.V1;
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

        public string GetAccountName()
        {
            return string.Empty;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(GenerateSignedUrl generateSignedUrl)
        {
            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(generateSignedUrl.Filename);

            var url = await GenerateSignedUrlAsync(generateSignedUrl.FilePath, mimeType);
            string? thumbnailUrl = null;

            if(!string.IsNullOrEmpty(generateSignedUrl.ThumbnailFilePath))
            {
                thumbnailUrl = await GenerateSignedUrlAsync(generateSignedUrl.ThumbnailFilePath, mimeType);
            }

            return new SignedUrlResponse
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
                    { "Content-Type", mimeType }
                },
                DoesRequirePermissionUpdate = true
            };
        }

        private async Task<string> GenerateSignedUrlAsync(string filePath, string mimeType)
        {
            if(_urlSigner == null)
            {
                throw new InvalidOperationException();
            }

            var requestTemplate = UrlSigner.RequestTemplate
                .FromBucket(_configuration.StoreName)
                .WithHttpMethod(System.Net.Http.HttpMethod.Put)
                .WithObjectName(filePath)
                .WithRequestHeaders(new Dictionary<string, IEnumerable<string>>
                {
                    { "Content-Type", new [] { mimeType } }
                });

            var options = UrlSigner.Options.FromDuration(TimeSpan.FromMinutes(_configuration.UrlExpirationMinutes));
            options.WithSigningVersion(SigningVersion.V4);

            var url = await _urlSigner.SignAsync(requestTemplate, options);

            return url;
        }

        public async Task SetObjectPublic(string objectName)
        {
            var objectPath = Path.Combine(_configuration.BasePath, objectName);
            var storageObject = await _storageClient.GetObjectAsync(_configuration.StoreName, objectPath);

            await _storageClient.UpdateObjectAsync(storageObject, new UpdateObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead });
        }

        public async Task<bool> ExistsAsync(string objectPath)
        {
            try
            {
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

        public async Task<FileResult> CreateAsync(string objectPath, Stream stream)
        {
            var filename = Path.GetFileName(objectPath);
            var mimeType = MimeUtility.GetMimeMapping(filename);

            var uploadOptions = new UploadObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.PublicRead
            };

            var result = await _storageClient.UploadObjectAsync(_configuration.StoreName, objectPath, mimeType, stream, uploadOptions);

            return new FileResult
            {
                Filename = filename,
                Path = result.Name,
                MimeType = mimeType
            };
        }

        public async Task<bool> DeleteAsync(string objectPath)
        {
            try
            {
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
                    Filename = filename,
                    Path = item.Name,
                    MimeType = item.ContentType
                };
            }
        }
    }
}
