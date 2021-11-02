using Blogifier.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Shared
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureThumbnails(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(serivceProvider => new ThumbnailSetting
            {
                IsEnabled = configuration.GetValue<bool?>("Thumbnail:Enabled") ?? false,
                Width = configuration.GetValue<int?>("Thumbnail:Width") ?? 100,
                Height = configuration.GetValue<int?>("Thumbnail:Height") ?? 100
            });

            return services;
        }
    }
}
