using System.Collections.Generic;

namespace Blogifier.Shared
{
    public class PagedResult<T> where T : class
    {
        public InfinitePagingDescriptor PagingDescriptor { get; set; }

        public IList<T> Results { get; set; }
    }
}
