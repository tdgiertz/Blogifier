using Blogifier.Core.Data;
using Blogifier.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.GoogleCloudLogging;
using System;
using System.IO;
using System.Linq;

namespace Blogifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var configuration = services.GetService<IConfiguration>();
                var section = configuration.GetSection("Blogifier");

                if (section.GetValue<string>("DbProvider") != "MongoDb")
                {
                    try
                    {
                        var dbContext = services.GetRequiredService<AppDbContext>();
                        if (dbContext.Database.GetPendingMigrations().Any())
                            dbContext.Database.Migrate();
                    }
                    catch { }
                }

                section = configuration.GetSection("Log");

                var logConfig = new LoggerConfiguration();

#if DEBUG
                logConfig = logConfig
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
#else
                if (section.GetValue<string>("Provider") == "Google")
                {
                    var config = new GoogleCloudLoggingSinkOptions { ProjectId = section.GetValue<string>("Resource"), UseJsonOutput = true };
                    logConfig = logConfig
                        .Enrich.FromLogContext()
                        .WriteTo.GoogleCloudLogging(config);
                }
                else
                {
                    logConfig = logConfig
                        .Enrich.FromLogContext()
                        .WriteTo.Console();
                }

#endif

                Log.Logger = logConfig.CreateLogger();
            }

            try
            {
                Log.Information("Starting web host");
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    var baseConfig = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile("appsettings.Development.json")
                        .Build();
                    if (baseConfig.GetValue<string>("SecretVault:Provider") == "Google")
                    {
                        config.AddGoogleSecretsManager(baseConfig["SecretVault:Resource"]);
                    }
                })
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>();
                });
    }
}
