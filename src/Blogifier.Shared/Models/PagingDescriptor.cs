using System;
using System.Text.Json.Serialization;

namespace Blogifier.Shared
{
    public class PagingDescriptor
    {
        [JsonInclude]
        public int CurrentPage { get; private set; }
        [JsonInclude]
        public int PageCount { get; private set; }
        [JsonInclude]
        public int PageSize { get; private set; }
        [JsonInclude]
        public long TotalCount { get; private set; }
        public int Skip => CurrentPage * PageSize - PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < PageCount;
        public int StartIndex => CurrentPage * PageSize - PageSize + 1;
        public int EndIndex => CurrentPage * PageSize;

        private const int DefaultPageSize = 10;

        public PagingDescriptor()
        {
            SetCurrentPage(1);
            SetPageSize(DefaultPageSize);
        }

        public PagingDescriptor(int currentPage, int pageSize = DefaultPageSize)
        {
            SetCurrentPage(currentPage);
            SetPageSize(pageSize);
        }

        protected void UpdatePageCount()
        {
            if (PageSize == 0) return;
            if (TotalCount == 0)
            {
                PageCount = 1;
            }
            else
            {
                PageCount = (int)Math.Ceiling(TotalCount / (double)PageSize);
            }
        }

        public virtual void SetTotalCount(long totalCount)
        {
            if (totalCount < 0)
            {
                throw new ArgumentException("Argument must be positive", nameof(totalCount));
            }

            var currentStartIndex = StartIndex;
            TotalCount = totalCount;
            UpdatePageCount();

            if(currentStartIndex > TotalCount)
            {
                CurrentPage = PageCount;
            }
        }

        public void SetPageSize(int pageSize)
        {
            if (pageSize < 1)
            {
                throw new ArgumentException("Argument must be greater than 0", nameof(pageSize));
            }

            var currentStartIndex = StartIndex;
            PageSize = pageSize;
            UpdatePageCount();
            UpdateCurrentPage(currentStartIndex, pageSize);
        }

        protected void UpdateCurrentPage(int startIndex, int pageSize)
        {
            SetCurrentPage(startIndex / pageSize);
        }

        public void SetCurrentPage(int pageNumber)
        {
            if (pageNumber < 1)
            {
                CurrentPage = 1;
            }
            else if (pageNumber > PageCount)
            {
                CurrentPage = PageCount;
            }
            else
            {
                CurrentPage = pageNumber;
            }
        }

        public void GoToPreviousPage()
        {
            SetCurrentPage(CurrentPage - 1);
        }

        public void GoToNextPage()
        {
            SetCurrentPage(CurrentPage + 1);
        }
    }
}
