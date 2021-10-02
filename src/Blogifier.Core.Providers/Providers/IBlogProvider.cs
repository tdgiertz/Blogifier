using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface IBlogProvider
	{
		Task<Blog> GetBlog();
		Task<Blog> TryGetBlog();
		Task<List<Category>> GetBlogCategories();
		Task<BlogItem> GetBlogItem();
        Task<bool> AddAsync(Blog blog);
		Task<bool> Update(Blog blog);
	}
}
