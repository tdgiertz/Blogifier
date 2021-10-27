using Blazored.Modal;
using Blogifier.Admin.Components;
using Blogifier.Admin.Serivces;
using Blogifier.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sotsera.Blazor.Toaster.Core.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Admin
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            };

            using var response = await httpClient.GetAsync("appsettings.json");
            using var stream = await response.Content.ReadAsStreamAsync();

            builder.Configuration.AddJsonStream(stream);

			builder.Services.AddLocalization();
            builder.Services.AddBlazoredModal();
			builder.Services.AddOptions();
			builder.Services.AddAuthorizationCore();

			builder.Services.AddScoped(sp => httpClient);

			builder.Services.AddScoped<AuthenticationStateProvider, BlogAuthenticationStateProvider>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddTransient<EasyMdeWrapper>();

            if(builder.Configuration.GetValue<bool>("FileStore:ServerUpload"))
            {
                builder.Services.AddScoped<IUploadFileService, UploadFileService>();
            }
            else
            {
                builder.Services.AddScoped<IUploadFileService, SignedUploadFileService>();
            }

            builder.Services.ConfigureThumbnails(builder.Configuration);

			builder.Services.AddToaster(config =>
			{
				config.PositionClass = Defaults.Classes.Position.BottomRight;
				config.PreventDuplicates = true;
				config.NewestOnTop = false;
			});

            builder.Services.AddSingleton<BlogStateProvider>();

			await builder.Build().RunAsync();
		}
	}
}
