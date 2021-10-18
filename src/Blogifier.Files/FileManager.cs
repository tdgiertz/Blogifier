using Blogifier.Shared;
using Blogifier.Files.Providers;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using Blogifier.Shared.Models;

namespace Blogifier.Files
{
    public class FileManager : IFileManager
    {
        private readonly IFileStoreProvider _fileStoreProvider;
        private readonly IFileDescriptorProvider _fileDescriptorProvider;
        private readonly Models.FileStoreConfiguration _fileStoreConfiguration;

        public FileManager(IFileStoreProvider fileStoreProvider, IFileDescriptorProvider fileDescriptorProvider, Models.FileStoreConfiguration configuration)
        {
            _fileStoreProvider = fileStoreProvider;
            _fileDescriptorProvider = fileDescriptorProvider;
            _fileStoreConfiguration = configuration;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request)
        {
            var response = await _fileStoreProvider.GetSignedUrlAsync(request);

            var fileDescriptor = response.ToFileDescriptor();

            fileDescriptor.DateCreated = DateTime.UtcNow;
            fileDescriptor.Id = Guid.NewGuid();

            if(!await _fileDescriptorProvider.InsertAsync(fileDescriptor))
            {
                throw new Exception("Failed to create file descriptor");
            }

            var fileModel = fileDescriptor.ToFileModel();

            fileModel.Url = response.FileModel.Url;
            fileModel.RelativePath = response.FileModel.RelativePath;
            fileModel.MimeType = response.FileModel.MimeType;

            response.FileModel = fileModel;

            return response;
        }

        public async Task<PagedResult<FileModel>> GetPagedAsync(FileSearchModel searchModel)
        {
            var fileDescriptors = await _fileDescriptorProvider.GetPagedAsync(searchModel.PagingDescriptor, searchModel.SearchTerm);

            return new PagedResult<FileModel>
            {
                Results = fileDescriptors.Select(d => d.ToFileModel()).ToList(),
                PagingDescriptor = searchModel.PagingDescriptor
            };
        }

        public async Task<FileModel> GetAsync(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            return fileDescriptor.ToFileModel();
        }

        public async Task<FileModel> CreateAsync(IFormFile formFile)
        {
            FileModel fileModel;
            FileDescriptor fileDescriptor;
            try
            {
                var fileResult = await _fileStoreProvider.CreateAsync(formFile);
                fileDescriptor = fileResult.ToFileDescriptor();
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
                fileModel = fileDescriptor.ToFileModel();
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

        public async Task<bool> SetObjectPublic(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            if(fileDescriptor == null)
            {
                return false;
            }

            await _fileStoreProvider.SetObjectPublic(fileDescriptor.Filename);

            return true;
        }

        public async Task<bool> UpdateAsync(FileModel model)
        {
            var result = await _fileDescriptorProvider.UpdateAsync(model.ToFileDescriptor());

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var fileDescriptor = await _fileDescriptorProvider.GetAsync(id);

            var result = await _fileDescriptorProvider.DeleteAsync(id);

            if (result)
            {
                await _fileStoreProvider.DeleteAsync(fileDescriptor.Filename);
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
