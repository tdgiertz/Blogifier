using System;
using Blogifier.Core.Providers.EfCore.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Files;
using Blogifier.Files.Azure.Extensions;
using Blogifier.Files.Aws.Extensions;
using Blogifier.Files.Backblaze.Extensions;
using Blogifier.Files.FileSystem.Extensions;
using Blogifier.Files.Google.Extensions;
using Blogifier.Files.Models;
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

            var isFileSystem = false;

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
                services.UseFileSystemFileStore();
                isFileSystem = true;
            }

            var fileStoreConfig = GetFromConfiguration(section, isFileSystem);

            services.AddSingleton(fileStoreConfig);
            services.AddScoped<IFileManager, FileManager>();

            return services;
        }

        private static FileStoreConfiguration GetFromConfiguration(IConfigurationSection section, bool isFileSystem)
        {
            var configuration = new FileStoreConfiguration
            {
                AuthenticationKey = section["AuthenticationKey"],
                AuthenticationKeyId = section["AuthenticationKeyId"],
                Endpoint = section["Endpoint"],
                AuthenticationKeySource = Enum.Parse<KeySource>(section["KeySource"]),
                BasePathTemplate = section["BasePathTemplate"],
                ThumbnailBasePathTemplate = section["ThumbnailBasePathTemplate"],
                StoreName = section["StoreName"],
                PublicUrlTemplate = section["PublicUrlTemplate"],
                AccountId = section["AccountId"]
            };

            if(isFileSystem && string.IsNullOrEmpty(configuration.BasePathTemplate))
            {
                configuration.BasePathTemplate = "data";
            }

            if(int.TryParse(section["UrlExpirationMinutes"], out var urlExpirationMinutes))
            {
                configuration.UrlExpirationMinutes = urlExpirationMinutes;
            }
            else
            {
                configuration.UrlExpirationMinutes = 60;
            }

            return configuration;
        }
    }
}
