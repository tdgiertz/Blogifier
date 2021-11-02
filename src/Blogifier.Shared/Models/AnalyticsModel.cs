namespace Blogifier.Shared
{
	public class AnalyticsModel
	{
		public long TotalPosts { get; set; }
        public long TotalPages { get; set; }
		public long TotalViews { get; set; }
		public long TotalSubscribers { get; set; }

        public AnalyticsListType DisplayType { get; set; }
        public AnalyticsPeriod DisplayPeriod { get; set; }

		public BarChartModel LatestPostViews { get; set; }
	}
}
