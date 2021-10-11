using Blogifier.Shared;
using Blogifier.Files.Providers;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using Serilog;

namespace Blogifier.Files
{
    public class FileManager : IFileManager
    {
        private readonly IFileStoreProvider _fileStoreProvider;
        private readonly IFileDescriptorProvider _fileDescriptorProvider;

        public FileManager(IFileStoreProvider fileStoreProvider, IFileDescriptorProvider fileDescriptorProvider)
        {
            _fileStoreProvider = fileStoreProvider;
            _fileDescriptorProvider = fileDescriptorProvider;
        }

        public async Task<PagedResult<FileModel>> GetPagedAsync(FileSearchModel searchModel)
        {
            var fileDescriptors = await _fileDescriptorProvider.GetPagedAsync(searchModel.PagingDescriptor, searchModel.SearchTerm);

            return new PagedResult<FileModel>
            {
                Results = fileDescriptors.Select(d => ToFileModel(d)).ToList(),
                PagingDescriptor = searchModel.PagingDescriptor
            };
        }

        public async Task<FileModel> GetAsync(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            return ToFileModel(fileDescriptor);
        }

        private static FileModel ToFileModel(FileDescriptor fileDescriptor)
        {
            return new FileModel
            {
                Id = fileDescriptor.Id,
                Filename = fileDescriptor.Filename,
                MimeType = fileDescriptor.MimeType,
                Url = fileDescriptor.Url,
                Description = fileDescriptor.Description,
                DateCreated = fileDescriptor.DateCreated,
                DateUpdated = fileDescriptor.DateUpdated,
                IsSuccessful = true
            };
        }

        private static FileDescriptor ToFileDescriptor(FileModel fileModel)
        {
            return new FileDescriptor
            {
                Id = fileModel.Id,
                Filename = fileModel.Filename,
                MimeType = fileModel.MimeType,
                Url = fileModel.Url,
                Description = fileModel.Description,
                DateCreated = fileModel.DateCreated,
                DateUpdated = fileModel.DateUpdated
            };
        }

        private static FileDescriptor ToFileDescriptor(FileResult fileResult)
        {
            return new FileDescriptor
            {
                Id = Guid.NewGuid(),
                Filename = fileResult.Filename,
                MimeType = fileResult.MimeType,
                Url = fileResult.Url,
                DateCreated = DateTime.UtcNow
            };
        }

        public async Task<FileModel> CreateAsync(IFormFile formFile)
        {
            FileModel fileModel;
            FileDescriptor fileDescriptor;
            try
            {
                var fileResult = await _fileStoreProvider.CreateAsync(formFile);
                fileDescriptor = ToFileDescriptor(fileResult);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save file");
                fileModel = new FileModel
                {
                    IsSuccessful = false,
                    Message = "Failed to save file"
                };
                return fileModel;
            }
            try
            {
                await _fileDescriptorProvider.InsertAsync(fileDescriptor);
                fileModel = ToFileModel(fileDescriptor);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to write file descriptor");
                fileModel = new FileModel
                {
                    IsSuccessful = false,
                    Message = "File saved. Failed to write file descriptor."
                };
                return fileModel;
            }

            return fileModel;
        }

        public async Task<bool> UpdateAsync(FileModel model)
        {
            var result = await _fileDescriptorProvider.UpdateAsync(ToFileDescriptor(model));

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            var result = await _fileStoreProvider.DeleteAsync(fileDescriptor.Filename);

            if (result)
            {
                result = await _fileDescriptorProvider.DeleteAsync(id);
            }

            return result;
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

            var removeFileModels = toRemove.Select(f => new SyncronizationModel
            {
                Filename = f.Filename,
                MimeType = f.MimeType,
                Url = f.Url,
                DateCreated = f.DateCreated,
                Description = f.Description
            });

            var importFileModels = importList.Select(i => new SyncronizationModel
            {
                Filename = i.Filename,
                MimeType = i.MimeType,
                Url = i.Url
            });

            return new FileSyncronizationModel
            {
                ImportFileModels = importFileModels,
                RemoveFileModels = removeFileModels
            };
        }
    }
}
