using Blogifier.Files.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Files.Aws.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseAwsFileStore(this IServiceCollection services)
        {
            services.AddScoped<IFileStoreProvider, AwsFileStoreProvider>();

            return services;
        }
    }
}
