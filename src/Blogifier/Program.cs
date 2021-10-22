﻿using Blogifier.Core.Data;
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

                section = configuration.GetSection("Log");

                if(section.GetValue<string>("Provider") == "Google")
                {
                    config = new GoogleCloudLoggingSinkOptions { ProjectId = section.GetValue<string>("Resource"), UseJsonOutput = true };
                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .WriteTo.GoogleCloudLogging(config)
                        .CreateLogger();
                }
                else
                {
                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .CreateLogger();
                }
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
                        .Build();
                    config.AddGoogleSecretsManager(baseConfig["SecretVault:Resource"]);
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
