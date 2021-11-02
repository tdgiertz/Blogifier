using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface ICategoryProvider
	{
        Task<List<CategoryItem>> Categories();
        Task<List<CategoryItem>> GetPublishedCategories();
        Task<List<Category>> GetCategoriesAsync();
        Task<List<CategoryItem>> SearchCategories(string term);
        Task<Category> GetCategory(Guid categoryId);
        Task<ICollection<Category>> GetPostCategories(Guid postId);

        Task<bool> SaveCategory(Category category);
        Task<Category> SaveCategory(string tag);

        Task<bool> AddPostCategory(Guid postId, string tag);
        Task<bool> SavePostCategories(Guid postId, List<Category> categories);

        Task<bool> RemoveCategory(Guid categoryId);
    }
}
