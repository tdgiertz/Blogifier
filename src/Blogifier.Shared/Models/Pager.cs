namespace Blogifier.Shared
{
    public class Pager : PagingDescriptor
    {
        public Pager(int currentPage, int pageSize = 0) : base(currentPage, pageSize)
        {
            if (PageSize == 0)
            {
                SetPageSize(10);
            }

            Newer = CurrentPage - 1;
            ShowNewer = CurrentPage > 1 ? true : false;

            Older = currentPage + 1;
        }

        public override void SetTotalCount(long totalCount)
        {
            if (totalCount == 0)
            {
                return;
            }

            base.SetTotalCount(totalCount);

            var lastItem = CurrentPage * PageSize;
            ShowOlder = totalCount > lastItem ? true : false;
            if (CurrentPage < 1 || lastItem > totalCount + PageSize)
            {
                NotFound = true;
            }
        }

        public bool NotFound { get; set; }

        public int Newer { get; set; }
        public bool ShowNewer { get; set; }

        public int Older { get; set; }
        public bool ShowOlder { get; set; }

        public string LinkToNewer { get; set; }
        public string LinkToOlder { get; set; }

        public string RouteValue { get; set; }
    }
}
