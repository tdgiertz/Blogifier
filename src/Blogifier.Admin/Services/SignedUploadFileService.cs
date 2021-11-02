using Blogifier.Admin.Models;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Blogifier.Shared.Resources;
using Microsoft.Extensions.Localization;
using Sotsera.Blazor.Toaster;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Admin.Serivces
{
    public class SignedUploadFileService : BaseUploadFileService, IUploadFileService
    {
        public SignedUploadFileService(IFileService fileService, HttpClient httpClient, IToaster toaster, IStringLocalizer<Resource> localizer, ThumbnailSetting thumbnailSetting) : base(fileService, httpClient, toaster, localizer, thumbnailSetting)
        {
        }

        public async Task UploadAsync(UploadFileModel uploadFileModel, Action onUpdateState, Action<bool> setIsEditing = null)
        {
            try
            {
                if (uploadFileModel.FileModel?.HasErrors ?? false)
                {
                    uploadFileModel.UploadState.IsUploadPending = true;
                    return;
                }

                var signedUrlResponse = await _fileService.GetSignedUrlAsync(uploadFileModel);

                uploadFileModel.FileModel = signedUrlResponse.FileModel;
                uploadFileModel.UploadState.IsUploading = true;

                setIsEditing?.Invoke(false);

                var thumbnailTask = string.IsNullOrEmpty(signedUrlResponse.ThumbnailUrl)
                    ? Task.CompletedTask
                    : UploadFileAsync(uploadFileModel.ThumbnailStream, uploadFileModel, signedUrlResponse.ThumbnailUrl, signedUrlResponse.Parameters);

                var fileTask = UploadFileWithProgressAsync(uploadFileModel.FileStream, uploadFileModel, signedUrlResponse.Url, signedUrlResponse.Parameters, onUpdateState);

                await Task.WhenAll(fileTask, thumbnailTask);

                uploadFileModel.FileModel.HasErrors |= !(await fileTask);

                if(signedUrlResponse.DoesRequirePermissionUpdate)
                {
                    var result = await _fileService.SetPublicAsync(signedUrlResponse.FileModel.Id);

                    if(!result.HasValue || !result.Value)
                    {
                        uploadFileModel.FileModel.HasErrors = true;
                        uploadFileModel.FileModel.Message = "Failed to set file permissions";
                    }
                }

                if(uploadFileModel.UploadState.IsUploadPending)
                {
                    await _fileService.SaveAsync(uploadFileModel.FileModel);
                }

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

        private async Task<bool> UploadFileWithProgressAsync(System.IO.Stream stream, UploadFileModel model, string url, IDictionary<string, string> headers, Action onUpdateState)
        {
            Action<long, double> onProgress = (long length, double percentage) =>
            {
                model.UploadState.ProgressPercent = (int)percentage;
                onUpdateState();
            };

            var content = new ProgressiveStreamContent(stream, 32 * 1024, onProgress);

            foreach (var header in headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }

            var client = new HttpClient
            {
                Timeout = TimeSpan.FromHours(8)
            };

            var response = await client.PutAsync(url, content);

            return response.IsSuccessStatusCode;
        }

        private async Task<bool> UploadFileAsync(System.IO.Stream stream, UploadFileModel model, string url, IDictionary<string, string> headers)
        {
            var content = new StreamContent(stream, 32 * 1024);

            foreach (var header in headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }

            var client = new HttpClient
            {
                Timeout = TimeSpan.FromHours(8)
            };

            var response = await client.PutAsync(url, content);

            return response.IsSuccessStatusCode;
        }
    }
}
