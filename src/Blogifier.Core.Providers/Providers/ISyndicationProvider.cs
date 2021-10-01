using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface ISyndicationProvider
	{
		Task<List<Post>> GetPosts(string feedUrl, Guid userId, Uri baseUrl, string webRoot = "/");
		Task<bool> ImportPost(Post post);
	}
}
