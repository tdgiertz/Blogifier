using Blogifier.Shared;
using Blogifier.Files.Providers;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;

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

        public async Task<FileListModel> GetPagedAsync(FileSearchModel searchModel)
        {
            var fileDescriptors = await _fileDescriptorProvider.GetPagedAsync(searchModel.Pager, searchModel.SearchTerm);

            return  new FileListModel
            {
                FileModels = fileDescriptors.Select(d => ToFileModel(d)),
                Pager = searchModel.Pager
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
                FileName = fileDescriptor.Filename,
                MimeType = fileDescriptor.MimeType,
                Uri = fileDescriptor.Url,
                Description = fileDescriptor.Description,
                DateCreated = fileDescriptor.DateCreated,
                DateUpdated = fileDescriptor.DateUpdated
            };
        }

        private static FileDescriptor ToFileDescriptor(FileModel fileModel)
        {
            return new FileDescriptor
            {
                Id = fileModel.Id,
                Filename = fileModel.FileName,
                MimeType = fileModel.MimeType,
                Url = fileModel.Uri,
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
            var fileResult = await _fileStoreProvider.CreateAsync(formFile);
            var fileDescriptor = ToFileDescriptor(fileResult);
            await _fileDescriptorProvider.InsertAsync(fileDescriptor);

            return ToFileModel(fileDescriptor);
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
