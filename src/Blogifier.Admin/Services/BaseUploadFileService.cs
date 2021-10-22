using Blogifier.Admin.Models;
using Blogifier.Shared.Models;
using Blogifier.Shared.Resources;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Sotsera.Blazor.Toaster;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Admin.Serivces
{
    public abstract class BaseUploadFileService
    {
        private readonly ThumbnailSetting _thumbnailSetting;
        protected readonly IFileService _fileService;
        protected readonly HttpClient _httpClient;
        protected readonly IToaster _toaster;
        protected readonly IStringLocalizer<Resource> _localizer;

        public BaseUploadFileService(IFileService fileService, HttpClient httpClient, IToaster toaster, IStringLocalizer<Resource> localizer, ThumbnailSetting thumbnailSetting)
        {
            _fileService = fileService;
            _httpClient = httpClient;
            _toaster = toaster;
            _localizer = localizer;
            _thumbnailSetting = thumbnailSetting;
        }

        public async IAsyncEnumerable<UploadFileModel> GetUploadFileModelsAsync(IReadOnlyList<UploadFileModel> files)
        {
            foreach (var file in files)
            {
                yield return await Task.FromResult(file);
            }
        }

        public async Task<List<UploadFileModel>> SetupNewFilesAsync(IEnumerable<IBrowserFile> files, Action<UploadFileModel> onEachModel)
        {
            var uploadFileModels = new List<UploadFileModel>();

            foreach (var file in files)
            {
                var uploadFileModel = new UploadFileModel
                {
                    FileStream = file.OpenReadStream(long.MaxValue),
                    FileModel = new()
                    {
                        Filename = file.Name
                    },
                    UploadState = new()
                    {
                        IsEditing = true
                    }
                };

                if(_thumbnailSetting.IsEnabled && IsThumbnailType(file.ContentType))
                {
                    var thumbnailStream = await file.RequestImageFileAsync(file.ContentType, _thumbnailSetting.Width, _thumbnailSetting.Height);
                    if(thumbnailStream != null)
                    {
                        uploadFileModel.ThumbnailStream = thumbnailStream.OpenReadStream(long.MaxValue);
                    }
                }

                onEachModel(uploadFileModel);

                uploadFileModels.Add(uploadFileModel);

                var alreadyExists = await _fileService.FileExistsAsync(file.Name);

                if (!alreadyExists.HasValue)
                {
                    uploadFileModel.FileModel.HasErrors = true;
                    uploadFileModel.FileModel.Message = "Unable to determine if file already exists.";
                    continue;
                }

                if (alreadyExists.HasValue && alreadyExists.Value)
                {
                    uploadFileModel.FileModel.HasErrors = true;
                    uploadFileModel.FileModel.Message = "File already exists. Change filename or save to overwrite.";
                    continue;
                }
            }

            return uploadFileModels;
        }

        private static bool IsThumbnailType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType) && contentType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
