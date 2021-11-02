using System.Collections.Generic;

namespace Blogifier.Shared
{
    public class PagedResult<T> where T : class
    {
        public PagingDescriptor PagingDescriptor { get; set; }

        public IList<T> Results { get; set; }
    }
}
