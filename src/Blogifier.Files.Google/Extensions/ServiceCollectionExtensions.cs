using Blogifier.Files.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Files.Google.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseGoogleFileStore(this IServiceCollection services)
        {
            services.AddScoped<IFileStoreProvider, GoogleFileStoreProvider>();

            return services;
        }
    }
}
