using Blogifier.Admin.Models;
using Blogifier.Shared;
using Blogifier.Shared.Models;
using Blogifier.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Sotsera.Blazor.Toaster;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Blogifier.Admin.Serivces
{
    public class FileService : IFileService
    {
        private readonly HttpClient _httpClient;
        private readonly IToaster _toaster;
        private readonly IStringLocalizer<Resource> _localizer;

        public FileService(HttpClient httpClient, IToaster toaster, IStringLocalizer<Resource> localizer)
        {
            _httpClient = httpClient;
            _toaster = toaster;
            _localizer = localizer;
        }

        public async Task<IList<FileModel>> CreateAsync(MultipartFormDataContent content)
        {
            var response = await _httpClient.PostAsync("api/file", content);

            var results = await response.Content.ReadFromJsonAsync<IList<FileModel>>();

            return results;
        }

        public async Task DeleteAsync(Guid id, EventCallback<Guid> onDelete, Action<bool> setIsBusy = null)
        {
            setIsBusy?.Invoke(true);

            try
            {
                var response = await _httpClient.DeleteAsync($"api/file/{id}");
                if (await HandleBoolResult(response))
                {
                    await onDelete.InvokeAsync(id);
                }
            }
            catch (Exception)
            {
                _toaster.Error(_localizer["generic-error"]);
            }

            setIsBusy?.Invoke(false);
        }

        public async Task SaveAsync(FileModel fileModel, Action<bool> setIsBusy = null, Action<bool> setIsEditing = null)
        {
            setIsBusy?.Invoke(true);

            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/file", fileModel);
                if (await HandleBoolResult(response))
                {
                    setIsEditing?.Invoke(false);
                }
            }
            catch (Exception)
            {
                _toaster.Error(_localizer["generic-error"]);
            }

            setIsBusy?.Invoke(false);
        }

        private async Task<bool> HandleBoolResult(HttpResponseMessage response)
        {
            var isSuccessful = false;
            if (response.IsSuccessStatusCode)
            {
                isSuccessful = await response.Content.ReadFromJsonAsync<bool>();
            }
            if (isSuccessful)
            {
                _toaster.Success(_localizer["completed"]);
            }
            else
            {
                _toaster.Error(_localizer["generic-error"]);
            }

            return isSuccessful;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(UploadFileModel uploadFileModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/file/signedUrl", new SignedUrlRequest
            {
                Filename = uploadFileModel.FileModel.Filename,
                ShouldGenerateThumbnailUrl = uploadFileModel.ThumbnailStream != null
            });

            return await response.Content.ReadFromJsonAsync<SignedUrlResponse>();
        }

        public async Task<bool?> FileExistsAsync(string filename)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<bool>($"api/file/exists/{filename}");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<PagedResult<FileModel>> GetListAsync(FileSearchModel fileSearchModel)
        {
            fileSearchModel.PagingDescriptor ??= new InfinitePagingDescriptor { PageSize = 15 };
            var response = await _httpClient.PostAsJsonAsync($"api/file/list/", fileSearchModel);
            return await response.Content.ReadFromJsonAsync<PagedResult<FileModel>>();
        }

        public async Task<bool?> SetPublicAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<bool>($"api/file/setpublic/{id}");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
