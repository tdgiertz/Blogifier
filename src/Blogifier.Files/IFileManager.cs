using Blogifier.Shared;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Blogifier.Files
{
    public interface IFileManager
    {
        Task<PagedResult<FileModel>> GetPagedAsync(FileSearchModel searchModel);
        Task<FileModel> GetAsync(Guid id);
        Task<FileModel> CreateAsync(IFormFile formFile);
        Task<bool> UpdateAsync(FileModel model);
        Task<bool> DeleteAsync(Guid id);
        Task<FileSyncronizationModel> SyncronizeAsync();
    }
}
