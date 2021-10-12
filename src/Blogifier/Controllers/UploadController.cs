using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Files;
using Blogifier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blogifier.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IFileManager _fileManager;

        public UploadController(ILogger<UploadController> logger, IFileManager fileManager)
        {
            _logger = logger;
            _fileManager = fileManager;
        }

        [HttpPost, Route("List")]
        public async Task<ActionResult<PagedResult<FileModel>>> List([FromBody] FileSearchModel fileSearchModel)
        {
            return await _fileManager.GetPagedAsync(fileSearchModel);
        }

        [HttpPost]
        public async Task<ActionResult<IList<FileModel>>> Post([FromForm] IEnumerable<IFormFile> files)
        {
            var models = new List<FileModel>();

            foreach (var file in files)
            {
                FileModel model;
                try
                {
                    model = await _fileManager.CreateAsync(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Post file failed");
                    model = new FileModel
                    {
                        IsSuccessful = false,
                        Message = "File upload failed"
                    };
                }

                models.Add(model);
            }

            return models;
        }

        [HttpPut]
        public async Task<ActionResult<bool>> Put([FromBody] FileModel fileModel)
        {
            return await _fileManager.UpdateAsync(fileModel);
        }

        [HttpDelete("{id:Guid}")]
        public async Task<ActionResult<bool>> Delete(Guid id)
        {
            return await _fileManager.DeleteAsync(id);
        }

        [HttpGet, Route("Syncronize")]
        public async Task<ActionResult<FileSyncronizationModel>> Syncronize()
        {
            return await _fileManager.SyncronizeAsync();
        }
    }
}
