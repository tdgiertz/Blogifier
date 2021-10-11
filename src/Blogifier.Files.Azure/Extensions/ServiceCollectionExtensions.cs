using Blogifier.Files.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Files.Azure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseAzureFileStore(this IServiceCollection services)
        {
            services.AddScoped<IFileStoreProvider, AzureFileStoreProvider>();

            return services;
        }
    }
}
