using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface IAnalyticsProvider
	{
		Task<AnalyticsModel> GetAnalytics();
        Task<bool> SaveDisplayType(int type);
        Task<bool> SaveDisplayPeriod(int period);
    }
}
