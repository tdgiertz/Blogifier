using System;
using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using MongoDB.Driver;

namespace Blogifier.Core.Providers.MongoDb.Extensions
{
    public static class IMongoDatabaseExtensions
    {
        public static IMongoCollection<T> GetNamedCollection<T>(this IMongoDatabase db)
        {
            var databaseName = GetTypeCollectionName(typeof(T));

            return db.GetCollection<T>(databaseName);
        }

        public static string GetTypeCollectionName<T>() => GetTypeCollectionName(typeof(T));

        private static string GetTypeCollectionName(Type type)
        {
            switch (type.Name)
            {
                case nameof(Author):
                    return "Author";
                case nameof(MongoBlog):
                    return "Blog";
                case nameof(FileDescriptor):
                    return "FileDescriptor";
                case nameof(MailSetting):
                    return "MailSetting";
                case nameof(Newsletter):
                    return "Newsletter";
                case nameof(Post):
                    return "Post";
                case nameof(Subscriber):
                    return "Subscriber";
            }

            throw new NotSupportedException($"Type \"{type.Name}\" is not supported.");
        }
    }
}
