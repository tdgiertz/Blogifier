using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;

namespace Blogifier.Core.Providers.EfCore
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        private PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            TotalCount = count;
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, PagingDescriptor pagingDescriptor)
        {
            var count = await source.CountAsync();
            var items = await source.Skip(pagingDescriptor.Skip).Take(pagingDescriptor.PageSize).ToListAsync();
            var pageIndex = (int)(pagingDescriptor.Skip / pagingDescriptor.PageSize) + 1;

            var result = new PaginatedList<T>(items, count, pageIndex, pagingDescriptor.PageSize);

            pagingDescriptor.SetTotalCount(result.TotalCount);

            return result;
        }
    }
}
