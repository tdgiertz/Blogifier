using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
	public class AnalyticsProvider : IAnalyticsProvider
	{
        private readonly IBlogProvider _blogProvider;
        private readonly IMongoCollection<Post> _postCollection;
        private readonly IMongoCollection<Subscriber> _subscriberCollection;
        private readonly IMongoCollection<MongoBlog> _blogCollection;

		public AnalyticsProvider(IMongoDatabase db, IBlogProvider blogProvider)
		{
            _postCollection = db.GetNamedCollection<Post>();
            _subscriberCollection = db.GetNamedCollection<Subscriber>();
            _blogCollection = db.GetNamedCollection<MongoBlog>();
            _blogProvider = blogProvider;
		}

		public async Task<AnalyticsModel> GetAnalytics()
		{
            var blog = await _blogProvider.GetBlog();
            var totalViews = await _postCollection
                    .Aggregate()
                    .Group(p => p.Id, g => new
                    {
                        Count = g.Sum(p => p.PostViews)
                    }).FirstOrDefaultAsync();

			var model = new AnalyticsModel()
			{
                TotalPosts = await _postCollection.Find(p => p.PostType == PostType.Post).CountDocumentsAsync(),
                TotalPages = await _postCollection.Find(p => p.PostType == PostType.Page).CountDocumentsAsync(),
                TotalViews = totalViews?.Count ?? 0,
                TotalSubscribers = await _subscriberCollection.Find(_ => true).CountDocumentsAsync(),
				LatestPostViews = await GetLatestPostViews(),
                DisplayType = blog.AnalyticsListType > 0 ? (AnalyticsListType)blog.AnalyticsListType : AnalyticsListType.Graph,
                DisplayPeriod = blog.AnalyticsPeriod > 0 ? (AnalyticsPeriod)blog.AnalyticsPeriod : AnalyticsPeriod.Days7
			};

			return await Task.FromResult(model);
		}

        public async Task<bool> SaveDisplayType(int type)
        {
            var blog = await _blogProvider.GetBlog();
            var update = Builders<MongoBlog>.Update.Set(b => b.AnalyticsListType, type);
            var result = await _blogCollection.UpdateManyAsync(b => b.Id == blog.Id, update);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> SaveDisplayPeriod(int period)
        {
            var blog = await _blogProvider.GetBlog();
            var update = Builders<MongoBlog>.Update.Set(b => b.AnalyticsPeriod, period);
            var result = await _blogCollection.UpdateManyAsync(b => b.Id == blog.Id, update);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        private async Task<BarChartModel> GetLatestPostViews()
		{
            var blog = await _blogProvider.GetBlog();
            var period = blog.AnalyticsPeriod == 0 ? 3 : blog.AnalyticsPeriod;

            var posts = await _postCollection
                .Find(p => p.Published > DateTime.MinValue)
                .SortByDescending(p => p.Published)
                .Limit(GetDays(period))
                .ToListAsync();

            if (posts == null || posts.Count() < 3)
            {
				return null;
            }

			posts = posts.OrderBy(p => p.Published).ToList();

			return new BarChartModel()
			{
				Labels = posts.Select(p => p.Title).ToList(),
				Data = posts.Select(p => p.PostViews).ToList()
			};
		}

        private int GetDays(int id)
        {
            switch ((AnalyticsPeriod)id)
            {
                case AnalyticsPeriod.Today:
                    return 1;
                case AnalyticsPeriod.Yesterday:
                    return 2;
                case AnalyticsPeriod.Days7:
                    return 7;
                case AnalyticsPeriod.Days30:
                    return 30;
                case AnalyticsPeriod.Days90:
                    return 90;
                default:
                    throw new ApplicationException("Unknown analytics period");
            }
        }
	}
}
