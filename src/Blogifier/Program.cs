using Blogifier.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
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

            GoogleCloudLoggingSinkOptions config;

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

                config = new GoogleCloudLoggingSinkOptions { ProjectId = section.GetValue<string>("GcpProjectName"), UseJsonOutput = true };
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.GoogleCloudLogging(config)
                .CreateLogger();

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
