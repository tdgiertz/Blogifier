using Blogifier.Files.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Files.FileSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseFileSystemFileStore(this IServiceCollection services)
        {
            services.AddScoped<IFileStoreProvider, FileSystemFileStoreProvider>();

            return services;
        }
    }
}
