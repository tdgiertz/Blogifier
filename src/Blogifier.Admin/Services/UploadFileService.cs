using Blogifier.Admin.Models;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Blogifier.Shared.Resources;
using Microsoft.Extensions.Localization;
using Sotsera.Blazor.Toaster;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Admin.Serivces
{
    public class UploadFileService : BaseUploadFileService, IUploadFileService
    {
        public UploadFileService(IFileService fileService, HttpClient httpClient, IToaster toaster, IStringLocalizer<Resource> localizer, ThumbnailSetting thumbnailSetting) : base(fileService, httpClient, toaster, localizer, thumbnailSetting)
        {
        }


        public async Task UploadAsync(UploadFileModel uploadFileModel, Action onUpdateState, Action<bool> setIsEditing = null)
        {
            Action<long, double> onProgress = (long length, double percentage) =>
            {
                uploadFileModel.UploadState.ProgressPercent = (int)percentage;
                onUpdateState();
            };

            try
            {
                if (uploadFileModel.FileModel?.HasErrors ?? false)
                {
                    uploadFileModel.UploadState.IsUploadPending = true;
                    return;
                }

                uploadFileModel.UploadState.IsUploading = true;

                setIsEditing?.Invoke(false);

                var content = new MultipartFormDataContent();

                var fileContent = new ProgressiveStreamContent(uploadFileModel.FileStream, 32 * 1024, onProgress);

                content.Add(fileContent, "\"files\"", uploadFileModel.FileModel.Filename);

                var result = await _fileService.CreateAsync(content);

                uploadFileModel.FileModel = result.First();

                uploadFileModel.UploadState.IsUploading = false;
                uploadFileModel.UploadState.IsUploadPending = uploadFileModel.FileModel.HasErrors;

                onUpdateState();
                await Task.Yield();
            }
            catch (Exception)
            {
                _toaster.Error(_localizer["generic-error"]);

                if (uploadFileModel != null)
                {
                    uploadFileModel.FileModel ??= new();
                    uploadFileModel.UploadState.IsUploading = false;
                    uploadFileModel.FileModel.HasErrors = true;
                    uploadFileModel.FileModel.Message = "File upload failed";
                }
            }
        }
    }
}
