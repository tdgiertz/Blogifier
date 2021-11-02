using Blogifier.Files.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Files.Backblaze.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseBackblazeFileStore(this IServiceCollection services)
        {
            services.AddScoped<IFileStoreProvider, BackblazeFileStoreProvider>();

            return services;
        }
    }
}
