using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using MongoDB.Bson.Serialization;

namespace Blogifier.Core.Providers
{
    internal static class BsonClassMappings
    {
        public static void Register()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Author)))
            {
                BsonClassMap.RegisterClassMap<Author>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(a => a.Id);
                    m.UnmapProperty(a => a.Posts);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Blog)))
            {
                BsonClassMap.RegisterClassMap<Blog>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(a => a.Id);
                    m.UnmapProperty(a => a.Authors);
                    m.UnmapProperty(a => a.Posts);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Category)))
            {
                BsonClassMap.RegisterClassMap<Category>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(a => a.Id);
                    m.UnmapProperty(a => a.Posts);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(FileDescriptor)))
            {
                BsonClassMap.RegisterClassMap<FileDescriptor>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(a => a.Id);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(MailSetting)))
            {
                BsonClassMap.RegisterClassMap<MailSetting>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(p => p.Id);
                    m.UnmapProperty(m => m.Blog);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Newsletter)))
            {
                BsonClassMap.RegisterClassMap<Newsletter>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(a => a.Id);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Post)))
            {
                BsonClassMap.RegisterClassMap<Post>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(p => p.Id);
                    m.UnmapProperty(p => p.Author);
                    m.UnmapProperty(p => p.Blog);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Subscriber)))
            {
                BsonClassMap.RegisterClassMap<Subscriber>(m =>
                {
                    m.AutoMap();
                    m.MapIdProperty(a => a.Id);
                    m.UnmapProperty(a => a.Blog);
                });
            }
        }
    }
}
