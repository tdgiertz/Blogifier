using Blogifier.Admin.Models;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Admin.Serivces
{
    public interface IUploadFileService
    {
        Task<List<UploadFileModel>> SetupNewFilesAsync(IEnumerable<IBrowserFile> files, Action<UploadFileModel> onEachModel);
        IAsyncEnumerable<UploadFileModel> GetUploadFileModelsAsync(IReadOnlyList<UploadFileModel> files);
        Task UploadAsync(UploadFileModel uploadFileModel, Action onUpdateState, Action<bool> setIsEditing = null);
    }
}
