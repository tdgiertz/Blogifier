using Blogifier.Admin.Models;
using Blogifier.Shared;
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
    public class UploadFileService : IUploadFileService
    {
        private readonly IFileService _fileService;
        private readonly HttpClient _httpClient;
        private readonly IToaster _toaster;
        private readonly IStringLocalizer<Resource> _localizer;

        public UploadFileService(IFileService fileService, HttpClient httpClient, IToaster toaster, IStringLocalizer<Resource> localizer)
        {
            _fileService = fileService;
            _httpClient = httpClient;
            _toaster = toaster;
            _localizer = localizer;
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

                onEachModel(uploadFileModel);

                uploadFileModels.Add(uploadFileModel);

                var alreadyExists = await _fileService.FileExistsAsync(file.Name);

                if (!alreadyExists.HasValue)
                {
                    uploadFileModel.FileModel.IsSuccessful = false;
                    uploadFileModel.FileModel.Message = "Unable to determine if file already exists.";
                    continue;
                }

                if (alreadyExists.HasValue && alreadyExists.Value)
                {
                    uploadFileModel.FileModel.IsSuccessful = false;
                    uploadFileModel.FileModel.Message = "File already exists. Change filename or save to overwrite.";
                    continue;
                }
            }

            return uploadFileModels;
        }

        public async Task UploadWithSignedUrlAsync(UploadFileModel uploadFileModel, Action onUpdateState)
        {
            try
            {
                if (!(uploadFileModel.FileModel?.IsSuccessful ?? false))
                {
                    uploadFileModel.UploadState.IsUploadPending = true;
                    return;
                }

                var signedUrlResponse = await _fileService.GetSignedUrlAsync(uploadFileModel);

                uploadFileModel.FileModel = signedUrlResponse.FileModel;
                uploadFileModel.UploadState.IsUploading = true;

                onUpdateState();

                await UploadFileAsync(uploadFileModel.FileStream, uploadFileModel, signedUrlResponse.Url, signedUrlResponse.Parameters, onUpdateState);

                uploadFileModel.UploadState.IsUploading = false;

                onUpdateState();

                await Task.Yield();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                _toaster.Error(_localizer["generic-error"]);

                if (uploadFileModel != null)
                {
                    uploadFileModel.FileModel ??= new();
                    uploadFileModel.UploadState.IsUploading = false;
                    uploadFileModel.FileModel.IsSuccessful = false;
                    uploadFileModel.FileModel.Message = "File upload failed";
                }
            }
        }

        private async Task<bool> UploadFileAsync(System.IO.Stream stream, UploadFileModel model, string url, IDictionary<string, string> headers, Action onUpdateState)
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
    }
}
