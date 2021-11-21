using System.Collections.Generic;

namespace Blogifier.Shared
{
    public class ListModel
    {
        public BlogItem Blog { get; set; }
        public Author Author { get; set; } // posts by author
        public string Category { get; set; } // posts by category

        public IEnumerable<PostItem> Posts { get; set; }
        public IEnumerable<PostItem> FeaturedPosts { get; set; }
        public InfinitePagingDescriptor PagingDescriptor { get; set; }

        public PostListType PostListType { get; set; }
    }
}
