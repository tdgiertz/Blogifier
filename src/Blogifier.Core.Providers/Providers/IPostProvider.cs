using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface IPostProvider
	{
		Task<List<Post>> GetPosts(PublishedStatus filter, PostType postType);
		Task<List<Post>> SearchPosts(string term);
		Task<Post> GetPostById(Guid id);
		Task<Post> GetPostBySlug(string slug);
		Task<string> GetSlugFromTitle(string title);
		Task<bool> Add(Post post);
		Task<bool> Update(Post post);
		Task<bool> Publish(Guid id, bool publish);
		Task<bool> Featured(Guid id, bool featured);
		Task<IEnumerable<PostItem>> GetPostItems();
		Task<PostModel> GetPostModel(string slug);
		Task<IEnumerable<PostItem>> GetPopular(Pager pager, Guid author = default(Guid));
		Task<IEnumerable<PostItem>> Search(Pager pager, string term, Guid author = default(Guid), string include = "", bool sanitize = false);
		Task<IEnumerable<PostItem>> GetList(Pager pager, Guid author = default(Guid), string category = "", string include = "", bool sanitize = true);
		Task<bool> Remove(Guid id);
	}
}
