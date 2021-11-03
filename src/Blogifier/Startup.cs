using Blogifier.Core.Extensions;
using Blogifier.Core.Providers;
using Blogifier.Extensions;
using Blogifier.Providers;
using Blogifier.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Blogifier
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var section = Configuration.GetSection("Blogifier");
            Log.Warning("Start configure services");

            services.AddDataProtection()
                .PersistKeysToGoogleCloudStorage(
                    Configuration["DataProtection:Bucket"],
                    Configuration["DataProtection:Object"])
                .ProtectKeysWithGoogleKms(
                    Configuration["DataProtection:KmsKeyName"]);

            services.AddLocalization(opts => { opts.ResourcesPath = ""; });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie();

            services.AddCors(o => o.AddPolicy("BlogifierPolicy", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
            services.AddBlogDatabase(Configuration);
            services.AddFileStore(Configuration);
            services.AddDataStore(Configuration);
            services.ConfigureThumbnails(Configuration);

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddAuthorization(option =>
            {
                option.AddPolicy("RequireRegistrationAuth", policy =>
                {
                    policy.AddRequirements(new RegistrationRequirement(Configuration));
                });
            });
            services.AddSingleton<IAuthorizationHandler, RegistrationRequirementHandler>();

            Log.Warning("Done configure services");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });

                app.UseExceptionHandler("/Error");
            }

            app.UseSerilogRequestLogging();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseCors("BlogifierPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                      name: "default",
                      pattern: "{controller=Home}/{action=Index}/{id?}"
                 );
                endpoints.MapRazorPages();
                endpoints.MapFallbackToFile("admin/{*path:nonfile}", "index.html");
                endpoints.MapFallbackToFile("account/{*path:nonfile}", "index.html");
            });
        }
    }
}
