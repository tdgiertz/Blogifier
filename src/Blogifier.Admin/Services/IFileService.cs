using Blogifier.Admin.Models;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Admin.Serivces
{
    public interface IFileService
    {
        Task<IList<FileModel>> CreateAsync(MultipartFormDataContent content);
        Task DeleteAsync(Guid id, EventCallback<Guid> onDelete, Action<bool> setIsBusy = null);
        Task SaveAsync(FileModel fileModel, Action<bool> setIsBusy = null, Action<bool> setIsEditing = null);
        Task<SignedUrlResponse> GetSignedUrlAsync(UploadFileModel uploadFileModel);
        Task<bool?> FileExistsAsync(string filename);
        Task<PagedResult<FileModel>> GetListAsync(FileSearchModel fileSearchModel);
        Task<bool?> SetPublicAsync(Guid id);
    }
}
