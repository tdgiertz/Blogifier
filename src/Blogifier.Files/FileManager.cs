using Blogifier.Shared;
using Blogifier.Files.Providers;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using Blogifier.Shared.Models;
using Blogifier.Files.Extensions;

namespace Blogifier.Files
{
    public class FileManager : IFileManager
    {
        private readonly ThumbnailSetting _thumbnailSetting;
        private readonly IFileStoreProvider _fileStoreProvider;
        private readonly IFileDescriptorProvider _fileDescriptorProvider;
        private readonly Models.FileStoreConfiguration _fileStoreConfiguration;

        public FileManager(IFileStoreProvider fileStoreProvider, IFileDescriptorProvider fileDescriptorProvider, Models.FileStoreConfiguration configuration, ThumbnailSetting thumbnailSetting)
        {
            _fileStoreProvider = fileStoreProvider;
            _fileDescriptorProvider = fileDescriptorProvider;
            _fileStoreConfiguration = configuration;
            _thumbnailSetting = thumbnailSetting;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request)
        {
            var objectPath = System.IO.Path.Combine(_fileStoreConfiguration.BasePath, request.Filename);
            var generateSignedUrl = new GenerateSignedUrl
            {
                Filename = request.Filename,
                FilePath = objectPath
            };

            if (request.ShouldGenerateThumbnailUrl)
            {
                generateSignedUrl.ThumbnailFilePath = System.IO.Path.Combine(_fileStoreConfiguration.ThumbnailBasePath, request.Filename);
            }

            var response = await _fileStoreProvider.GetSignedUrlAsync(generateSignedUrl);

            var fileDescriptor = response.ToFileDescriptor();

            fileDescriptor.RelativePath = System.IO.Path.Combine(_fileStoreConfiguration.BasePath, request.Filename);
            fileDescriptor.ThumbnailRelativePath = System.IO.Path.Combine(_fileStoreConfiguration.ThumbnailBasePath, request.Filename);
            fileDescriptor.DateCreated = DateTime.UtcNow;
            fileDescriptor.Id = Guid.NewGuid();

            if (!await _fileDescriptorProvider.InsertAsync(fileDescriptor))
            {
                throw new Exception("Failed to create file descriptor");
            }

            var fileModel = fileDescriptor.ToFileModel(GetUrl(fileDescriptor), GetThumbnailUrl(fileDescriptor));

            fileModel.Url = response.FileModel.Url;
            fileModel.RelativePath = response.FileModel.RelativePath;
            fileModel.MimeType = response.FileModel.MimeType;

            response.FileModel = fileModel;

            return response;
        }

        public async Task<PagedResult<FileModel>> GetPagedAsync(FileSearchModel searchModel)
        {
            var fileDescriptors = await _fileDescriptorProvider.GetPagedAsync(searchModel.PagingDescriptor, searchModel.SearchTerm);

            var accountName = _fileStoreProvider.GetAccountName();

            Func<FileDescriptor, string> getUrl = fd => _fileStoreConfiguration.ReplacePublicUrlTemplateValues(fd.Filename, fd.RelativePath, accountName);
            Func<FileDescriptor, string> getThumbnailUrl = fd => _fileStoreConfiguration.ReplacePublicUrlTemplateValues(fd.Filename, fd.ThumbnailRelativePath, accountName);

            return new PagedResult<FileModel>
            {
                Results = fileDescriptors.Select(d => d.ToFileModel(GetUrl(d), GetThumbnailUrl(d))).ToList(),
                PagingDescriptor = searchModel.PagingDescriptor
            };
        }

        public async Task<FileModel> GetAsync(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            var accountName = _fileStoreProvider.GetAccountName();

            return fileDescriptor.ToFileModel(GetUrl(fileDescriptor), GetThumbnailUrl(fileDescriptor));
        }

        public async Task<FileModel> GetAsync(string filename)
        {
            var filePath = System.IO.Path.Combine(_fileStoreConfiguration.BasePath, filename);
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(filePath);

            return fileDescriptor.ToFileModel(GetUrl(fileDescriptor), GetThumbnailUrl(fileDescriptor));
        }

        public async Task<FileModel> CreateAsync(IFormFile formFile)
        {
            FileModel fileModel;
            FileDescriptor fileDescriptor;
            try
            {
                var filePath = System.IO.Path.Combine(_fileStoreConfiguration.BasePath, formFile.FileName);
                var fileResult = await _fileStoreProvider.CreateAsync(filePath, formFile.OpenReadStream());
                string? thumbnailFilePath = null;

                var contentType = formFile.ContentType ?? MimeMapping.MimeUtility.GetMimeMapping(formFile.FileName);

                if(_thumbnailSetting.IsEnabled && (contentType?.StartsWith("image") ?? false))
                {
                    using var stream = formFile.OpenReadStream();
                    using var thumbnailStream = await ImageProcessor.ResizeImageAsync(stream, _thumbnailSetting.Width, _thumbnailSetting.Height);
                    thumbnailFilePath = System.IO.Path.Combine(_fileStoreConfiguration.ThumbnailBasePath, formFile.FileName);
                    thumbnailStream.Position = 0;
                    var thumbnailFileResult = await _fileStoreProvider.CreateAsync(thumbnailFilePath, thumbnailStream);
                }

                fileDescriptor = fileResult.ToFileDescriptor(thumbnailFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save file");
                fileModel = new FileModel
                {
                    Filename = formFile.FileName,
                    HasErrors = true,
                    Message = "Failed to save file"
                };
                return fileModel;
            }

            try
            {
                await _fileDescriptorProvider.InsertAsync(fileDescriptor);
                fileModel = fileDescriptor.ToFileModel(GetUrl(fileDescriptor), GetThumbnailUrl(fileDescriptor));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to write file descriptor");
                fileModel = new FileModel
                {
                    Filename = formFile.FileName,
                    HasErrors = true,
                    Message = "File saved. Failed to write file descriptor."
                };
                return fileModel;
            }

            return fileModel;
        }

        public async Task<bool> SetObjectPublic(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            if (fileDescriptor == null)
            {
                return false;
            }

            var fileTask = _fileStoreProvider.SetObjectPublic(fileDescriptor.RelativePath);
            var thumbnailTask = Task.CompletedTask;

            if (!string.IsNullOrEmpty(fileDescriptor.ThumbnailRelativePath))
            {
                thumbnailTask = _fileStoreProvider.SetObjectPublic(fileDescriptor.ThumbnailRelativePath);
            }

            await Task.WhenAll(fileTask, thumbnailTask);

            return true;
        }

        public async Task<bool> UpdateAsync(FileModel model)
        {
            model.DateUpdated = DateTime.UtcNow;
            var result = await _fileDescriptorProvider.UpdateAsync(model.ToFileDescriptor());

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            var result = await _fileDescriptorProvider.DeleteAsync(id);

            if (result)
            {
                var fileTask = _fileStoreProvider.DeleteAsync(fileDescriptor.RelativePath);
                var thumbnailTask = Task.CompletedTask;

                if (!string.IsNullOrEmpty(fileDescriptor.ThumbnailRelativePath))
                {
                    thumbnailTask = _fileStoreProvider.DeleteAsync(fileDescriptor.ThumbnailRelativePath);
                }

                await Task.WhenAll(fileTask, thumbnailTask);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(string filename)
        {
            var filePath = System.IO.Path.Combine(_fileStoreConfiguration.BasePath, filename);
            return await _fileDescriptorProvider.ExistsAsync(filePath);
        }

        public async Task<FileSyncronizationModel> SyncronizeAsync()
        {
            var fileDescriptors = await _fileDescriptorProvider.ListAsync();
            var filenameLookup = fileDescriptors.ToDictionary(k => k.Filename, v => v);

            var importList = new List<FileResult>();

            var existingFiles = new HashSet<string>();

            await foreach (var file in _fileStoreProvider.ListAsync())
            {
                if (!filenameLookup.ContainsKey(file.Filename))
                {
                    importList.Add(file);
                }
                else
                {
                    existingFiles.Add(file.Filename);
                }
            }

            var toRemove = filenameLookup.Where(f => !existingFiles.Contains(f.Key)).Select(f => f.Value);

            var removeFileModels = toRemove.Select(fd => new SyncronizationModel
            {
                Filename = fd.Filename,
                MimeType = fd.MimeType,
                Url = GetUrl(fd),
                DateCreated = fd.DateCreated,
                Description = fd.Description
            });

            var importFileModels = importList.Select(fr => new SyncronizationModel
            {
                Filename = fr.Filename,
                MimeType = fr.MimeType,
                Url = fr.Url
            });

            return new FileSyncronizationModel
            {
                ImportFileModels = importFileModels,
                RemoveFileModels = removeFileModels
            };
        }

        private string GetUrl(FileDescriptor fileDescriptor)
        {
            var accountName = _fileStoreProvider.GetAccountName();
            return _fileStoreConfiguration.ReplacePublicUrlTemplateValues(fileDescriptor.Filename, fileDescriptor.RelativePath, accountName);
        }

        private string GetThumbnailUrl(FileDescriptor fileDescriptor)
        {
            var accountName = _fileStoreProvider.GetAccountName();
            return _fileStoreConfiguration.ReplacePublicUrlTemplateValues(fileDescriptor.Filename, fileDescriptor.ThumbnailRelativePath, accountName);
        }
    }
}
