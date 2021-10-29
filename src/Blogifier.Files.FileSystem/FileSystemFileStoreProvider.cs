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
using Blogifier.Shared.Helpers;

namespace Blogifier.Files.FileSystem
{
    public class FileSystemFileStoreProvider : IFileStoreProvider
    {
        private readonly FileStoreConfiguration _configuration;

        public FileSystemFileStoreProvider(FileStoreConfiguration configuration)
        {
            configuration.BasePath ??= "";

            if(Path.IsPathRooted(configuration.BasePath))
            {
                throw new System.ArgumentException("Path should be relative", nameof(configuration.BasePath));
            }

            if (string.IsNullOrEmpty(configuration.PublicUrlTemplate))
            {
                throw new System.ArgumentException("Argument property required", $"{nameof(FileStoreConfiguration)}.{nameof(FileStoreConfiguration.PublicUrlTemplate)}");
            }
            if (string.IsNullOrEmpty(configuration.ThumbnailBasePath))
            {
                configuration.ThumbnailBasePath = Path.Combine(configuration.BasePath, "Thumbnails");
            }
            if(Path.IsPathRooted(configuration.ThumbnailBasePath))
            {
                throw new System.ArgumentException("Path should be relative", nameof(configuration.ThumbnailBasePath));
            }

            _configuration = configuration;
        }

        public string GetAccountName()
        {
            return string.Empty;
        }

        private string GetRootPath()
        {
            return Path.Combine(FileSystemHelpers.ContentRoot, "wwwroot");
        }

        public Task<SignedUrlResponse> GetSignedUrlAsync(GenerateSignedUrl generateSignedUrl)
        {
            throw new NotImplementedException();
        }

        private string GenerateSignedUrl(string filePath, string mimeType)
        {
            throw new NotImplementedException();
        }

        public async Task SetObjectPublic(string objectPath)
        {
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string objectPath)
        {
            var fullPath = Path.Combine(GetRootPath(), objectPath);
            return await Task.FromResult(File.Exists(fullPath));
        }

        public async Task<FileResult> CreateAsync(string objectPath, Stream stream)
        {
            var filename = Path.GetFileName(objectPath);
            var mimeType = MimeUtility.GetMimeMapping(filename);

            var fullPath = Path.Combine(GetRootPath(), objectPath);
            using var fileStream = new FileStream(fullPath, FileMode.Create);
            await stream.CopyToAsync(fileStream);

            var result = new FileResult
            {
                Filename = filename,
                Path = objectPath,
                MimeType = mimeType
            };

            return result;
        }

        public async Task<bool> DeleteAsync(string objectPath)
        {
            try
            {
                var fullPath = Path.Combine(GetRootPath(), objectPath);
                File.Delete(fullPath);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FileSystem DeleteAsync failed");
                return false;
            }
        }

        public async IAsyncEnumerable<FileResult> ListAsync()
        {
            var rootPath = GetRootPath();
            foreach(var file in Directory.GetFiles(_configuration.BasePath, "*", SearchOption.TopDirectoryOnly))
            {
                var filename = Path.GetFileName(file);
                var mimeType = MimeMapping.MimeUtility.GetMimeMapping(filename);
                var result = new FileResult
                {
                    Filename = filename,
                    Path =  file?.Replace(rootPath, ""),
                    MimeType = mimeType
                };
                yield return await Task.FromResult(result);
            }
        }
    }
}
