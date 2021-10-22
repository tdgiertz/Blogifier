using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using MimeMapping;
using Serilog;
using Blogifier.Shared.Models;

namespace Blogifier.Files.Aws
{
    public class AwsFileStoreProvider : IFileStoreProvider
    {
        private readonly FileStoreConfiguration _configuration;
        private readonly AmazonS3Client _storageClient;

        public AwsFileStoreProvider(FileStoreConfiguration configuration)
        {
            configuration.BasePath ??= "";
            CheckConfiguration(configuration);

            var endpoint = Amazon.RegionEndpoint.GetBySystemName(configuration.Endpoint);
            _storageClient = new AmazonS3Client(configuration.AuthenticationKeyId, configuration.AuthenticationKey, endpoint);

            _configuration = configuration;
        }

        public AwsFileStoreProvider(FileStoreConfiguration configuration, string serviceUrl)
        {
            configuration.BasePath ??= "";
            CheckConfiguration(configuration);

            var config = new AmazonS3Config
            {
                 ServiceURL = serviceUrl,
            };
            _storageClient = new AmazonS3Client(configuration.AuthenticationKeyId, configuration.AuthenticationKey, config);

            _configuration = configuration;
        }

        private static void CheckConfiguration(FileStoreConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.StoreName))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.StoreName)}");
            }
            if (string.IsNullOrEmpty(configuration.Endpoint))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.Endpoint)}");
            }
            if (string.IsNullOrEmpty(configuration.AuthenticationKeyId))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.AuthenticationKeyId)}");
            }
            if (string.IsNullOrEmpty(configuration.AuthenticationKey))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.AuthenticationKey)}");
            }
            if (string.IsNullOrEmpty(configuration.PublicUrlTemplate))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.PublicUrlTemplate)}");
            }
            if (string.IsNullOrEmpty(configuration.ThumbnailBasePath))
            {
                configuration.ThumbnailBasePath = Path.Combine(configuration.BasePath, "Thumbnails");
            }
        }

        public string GetAccountName()
        {
            return string.Empty;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(GenerateSignedUrl generateSignedUrl)
        {
            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(generateSignedUrl.Filename);

            var url = GenerateSignedUrl(generateSignedUrl.FilePath, mimeType);
            string? thumbnailUrl = null;

            if(!string.IsNullOrEmpty(generateSignedUrl.ThumbnailFilePath))
            {
                thumbnailUrl = GenerateSignedUrl(generateSignedUrl.ThumbnailFilePath, mimeType);
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
                    { "Content-Type", mimeType }
                },
                DoesRequirePermissionUpdate = false
            };

            return await Task.FromResult(response);
        }

        private string GenerateSignedUrl(string filePath, string mimeType)
        {
            var urlRequest = new GetPreSignedUrlRequest
            {
                Key = filePath,
                Verb = HttpVerb.PUT,
                BucketName = _configuration.StoreName,
                Expires = DateTime.Now.AddMinutes(_configuration.UrlExpirationMinutes),
                ContentType = mimeType
            };

            var url = _storageClient.GetPreSignedURL(urlRequest);

            return url;
        }

        public async Task SetObjectPublic(string objectPath)
        {
            var storageObject = await _storageClient.GetObjectAsync(_configuration.StoreName, objectPath);

            var aclRequest = new PutACLRequest
            {
                Key = objectPath,
                BucketName = _configuration.StoreName,
                CannedACL = S3CannedACL.PublicRead
            };
            await _storageClient.PutACLAsync(aclRequest);
        }

        public async Task<bool> ExistsAsync(string objectPath)
        {
            try
            {
                await _storageClient.GetObjectAsync(_configuration.StoreName, objectPath);

                return false;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
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

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = objectPath,
                BucketName = _configuration.StoreName,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = mimeType
            };
            var fileTransferUtility = new TransferUtility(_storageClient);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return new FileResult
            {
                Filename = filename,
                Path = objectPath,
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
                Log.Error(ex, "AWS DeleteObjectAsync failed");
                return false;
            }
        }

        public async IAsyncEnumerable<FileResult> ListAsync()
        {
            var paginator = _storageClient.Paginators.ListObjectsV2(new ListObjectsV2Request
            {
                BucketName = _configuration.StoreName,
                Delimiter = "/"
            });
            await foreach (var s3Object in paginator.S3Objects)
            {
                var filename = Path.GetFileName(s3Object.Key);
                var mimeType = MimeUtility.GetMimeMapping(filename);
                yield return new FileResult
                {
                    Filename = filename,
                    Path = s3Object.Key,
                    MimeType = mimeType
                };
            }
        }
    }
}
