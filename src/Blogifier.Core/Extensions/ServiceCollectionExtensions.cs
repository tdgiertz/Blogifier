using Blogifier.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;

namespace Blogifier.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlogDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("Blogifier");
            var conn = section.GetValue<string>("ConnString");

            if (section.GetValue<string>("DbProvider") == "SQLite")
                services.AddDbContext<AppDbContext>(o => o.UseSqlite(conn));

            if (section.GetValue<string>("DbProvider") == "SqlServer")
                services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn));

            if (section.GetValue<string>("DbProvider") == "Postgres")
                services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));

            if (section.GetValue<string>("DbProvider") == "MongoDb")
            {
                services.AddTransient<IMongoDatabase>(provider =>
                {
                    var dbName = section.GetValue<string>("MongoDbDatabaseName");
                    var client = new MongoClient(conn);

                    return client.GetDatabase(dbName);
                });
            }

            //TODO: this is not tested
            if (section.GetValue<string>("DbProvider") == "MySql")
            {
                services.AddDbContextPool<AppDbContext>(
                    dbContextOptions => dbContextOptions.UseMySql(
                        section.GetValue<string>("ConnString"),
                        new MySqlServerVersion(new Version(8, 0, 21)),
                        mySqlOptions => mySqlOptions.CharSetBehavior(CharSetBehavior.NeverAppend)
                    )
                );
            }
            services.AddDatabaseDeveloperPageExceptionFilter();
            return services;
        }
    }
}
