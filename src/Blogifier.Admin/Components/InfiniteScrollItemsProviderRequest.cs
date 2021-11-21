using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Blogifier.Admin.Components
{
    public delegate Task<ScrollItemsResponse<T>> ItemsProviderRequestDelegate<T>(ScrollItemsProviderRequest<T> context);
    public delegate Task<IEnumerable<int>> ItemsProvider<T>(ScrollItemsProviderRequest<T> request);

    public sealed class ScrollItemsProviderRequest<T>
    {
        public ScrollItemsProviderRequest(int startIndex, T model, CancellationToken cancellationToken)
        {
            StartIndex = startIndex;
            Model = model;
            CancellationToken = cancellationToken;
        }

        public int StartIndex { get; }
        public T Model { get; }
        public CancellationToken CancellationToken { get; }
    }

    public class ScrollItemsResponse<T>
    {
        public IList<T> Items { get; set; }
        public bool HasMoreItems { get; set; }
    }
}
