using Blogifier.Core.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StorageController : ControllerBase
	{
		private readonly IStorageProvider _storageProvider;
		private readonly IAuthorProvider _authorProvider;
		private readonly IBlogProvider _blogProvider;
		private readonly IPostProvider _postProvider;

        public StorageController(IStorageProvider storageProvider, IAuthorProvider authorProvider, IBlogProvider blogProvider, IPostProvider postProvider)
		{
			_storageProvider = storageProvider;
			_authorProvider = authorProvider;
			_blogProvider = blogProvider;
			_postProvider = postProvider;
		}

		[Authorize]
		[HttpGet("themes")]
		public async Task<IList<string>> GetThemes()
		{
			return await _storageProvider.GetThemes();
		}

		[Authorize]
		[HttpPut("exists")]
		public async Task<IActionResult> FileExists([FromBody] string path)
		{
			return (await Task.FromResult(_storageProvider.FileExists(path))) ? Ok() : BadRequest();
		}
	}
}
