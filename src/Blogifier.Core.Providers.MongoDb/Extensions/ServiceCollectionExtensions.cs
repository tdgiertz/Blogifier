using Microsoft.Extensions.DependencyInjection;

namespace Blogifier.Core.Providers.MongoDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoDbBlogProviders(this IServiceCollection services)
        {
            BsonClassMappings.Register();

            services.AddScoped<IAuthorProvider, AuthorProvider>();
            services.AddScoped<IBlogProvider, BlogProvider>();
            services.AddScoped<IPostProvider, PostProvider>();
            services.AddScoped<IStorageProvider, StorageProvider>();
            services.AddScoped<IFeedProvider, FeedProvider>();
            services.AddScoped<ICategoryProvider, CategoryProvider>();
            services.AddScoped<IAnalyticsProvider, AnalyticsProvider>();
            services.AddScoped<INewsletterProvider, NewsletterProvider>();
            services.AddScoped<IEmailProvider, MailKitProvider>();
            services.AddScoped<IThemeProvider, ThemeProvider>();
            services.AddScoped<ISyndicationProvider, SyndicationProvider>();
            services.AddScoped<IAboutProvider, AboutProvider>();

            return services;
        }
    }
}
