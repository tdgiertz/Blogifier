using System;
using Blogifier.Core.Providers.EfCore.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Files;
using Blogifier.Files.Azure.Extensions;
using Blogifier.Files.Aws.Extensions;
using Blogifier.Files.Backblaze.Extensions;
using Blogifier.Files.Google.Extensions;
using Blogifier.Files.Models;
using Blogifier.Files.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataStore(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("Blogifier");

            if (section.GetValue<string>("DbProvider") == "MongoDb")
            {
                services.AddMongoDbBlogProviders();
            }
            else
            {
                services.AddEfCoreBlogProviders();
            }

            return services;
        }

        public static IServiceCollection AddFileStore(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("FileStore");

            var provider = section.GetValue<string>("Provider");

            var fileStoreConfig = GetFromConfiguration(section);

            services.AddSingleton(fileStoreConfig);

            if (provider.Equals("Google", System.StringComparison.InvariantCultureIgnoreCase))
            {
                services.UseGoogleFileStore();
            }
            else if (provider.Equals("Azure", System.StringComparison.InvariantCultureIgnoreCase))
            {
                services.UseAzureFileStore();
            }
            else if (provider.Equals("Aws", System.StringComparison.InvariantCultureIgnoreCase))
            {
                services.UseAwsFileStore();
            }
            else if (provider.Equals("Backblaze", System.StringComparison.InvariantCultureIgnoreCase))
            {
                services.UseBackblazeFileStore();
            }
            else
            {
                services.AddScoped<IFileStoreProvider, FileStoreProvider>();
            }

            services.AddScoped<IFileManager, FileManager>();

            return services;
        }

        private static FileStoreConfiguration GetFromConfiguration(IConfigurationSection section)
        {
            return new FileStoreConfiguration
            {
                AuthenticationKey = section["AuthenticationKey"],
                AuthenticationKeyId = section["AuthenticationKeyId"],
                Endpoint = section["Endpoint"],
                AuthenticationKeySource = Enum.Parse<KeySource>(section["KeySource"]),
                BasePath = section["BasePath"],
                ThumbnailBasePath = section["ThumbnailBasePath"],
                StoreName = section["StoreName"],
                PublicUrlTemplate = section["PublicUrlTemplate"]
            };
        }
    }
}
