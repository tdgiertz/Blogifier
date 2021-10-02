using Blogifier.Core.Providers;
using Blogifier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CategoryController : ControllerBase
	{
		private readonly ICategoryProvider _categoryProvider;

		public CategoryController(ICategoryProvider categoryProvider)
		{
			_categoryProvider = categoryProvider;
		}

		[HttpGet("{postId:Guid}")]
		public async Task<ICollection<Category>> GetPostCategories(Guid postId)
		{
			return await _categoryProvider.GetPostCategories(postId);
		}

        [HttpGet("byId/{categoryId:Guid}")]
        public async Task<Category> GetCategory(Guid categoryId)
        {
            return await _categoryProvider.GetCategory(categoryId);
        }

        [HttpGet]
        public async Task<List<CategoryItem>> GetCategories()
        {
            return await _categoryProvider.Categories();
        }

        [HttpGet("{term}")]
        public async Task<List<CategoryItem>> SearchCategories(string term = "*")
        {
            return await _categoryProvider.SearchCategories(term);
        }

        [Authorize]
		[HttpPost("{postId:Guid}/{tag}")]
		public async Task<ActionResult<bool>> AddPostCategory(Guid postId, string tag)
		{
			return await _categoryProvider.AddPostCategory(postId, tag);
		}

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<bool>> SaveCategory(Category category)
        {
            return await _categoryProvider.SaveCategory(category);
        }

        [Authorize]
        [HttpPut("{postId:Guid}")]
        public async Task<ActionResult<bool>> SavePostCategories(Guid postId, List<Category> categories)
        {
            return await _categoryProvider.SavePostCategories(postId, categories);
        }

        [Authorize]
        [HttpDelete("{categoryId:Guid}")]
        public async Task<ActionResult<bool>> RemoveCategory(Guid categoryId)
        {
            return await _categoryProvider.RemoveCategory(categoryId);
        }
    }
}
