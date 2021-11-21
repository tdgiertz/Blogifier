using System;

namespace Blogifier.Shared
{
    public class InfinitePagingDescriptor
    {
        public DateTime? LastDateTime { get; set; }
        public string SearchTerm { get; set; }
        public string PagingUrl { get; set; }
        public int PageSize { get; set; }
        public bool HasMorePages { get; set; }
    }
}
