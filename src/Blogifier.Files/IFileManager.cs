using Blogifier.Shared;
using Blogifier.Shared.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Blogifier.Files
{
    public interface IFileManager
    {
        Task<string> GetBasePath();
        Task<string> GetThumbnailBasePath();
        Task<SignedUrlResponse> GetSignedUrlAsync(SignedUrlRequest request);
        Task<PagedResult<FileModel>> GetPagedAsync(FileSearchModel searchModel);
        Task<FileModel> GetAsync(Guid id);
        Task<FileModel> GetAsync(string filename);
        Task<FileModel> CreateAsync(IFormFile formFile);
        Task<bool> SetObjectPublic(Guid id);
        Task<bool> UpdateAsync(FileModel model);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(string filename);
        Task<FileSyncronizationModel> SyncronizeAsync();
    }
}
