using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Castle.Logging.Log4Net;
using Castle.Facilities.Logging;
using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using LotteryDetection.Configuration;
using LotteryDetection.Identity;
using LotteryDetection.Web.HealthCheck;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LotteryDetection.Web.Public.Startup;

public class Startup
{
    private readonly IConfigurationRoot _appConfiguration;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public Startup(IWebHostEnvironment env)
    {
        _hostingEnvironment = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(_appConfiguration);

        //MVC
        var mvcBuilder = services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });

#if DEBUG
        mvcBuilder.AddRazorRuntimeCompilation();
#endif

        if (bool.Parse(_appConfiguration["KestrelServer:IsEnabled"])) ConfigureKestrel(services);

        IdentityRegistrar.Register(services);
        services.AddSignalR();

        if (bool.Parse(_appConfiguration["HealthChecks:HealthChecksEnabled"])) ConfigureHealthChecks(services);

        //Configure Abp and Dependency Injection
        return services.AddAbp<LotteryDetectionWebFrontEndModule>(options =>
        {
            //Configure Log4Net logging
            options.IocManager.IocContainer.AddFacility<LoggingFacility>(f => f.UseAbpLog4Net().WithConfig(
                _hostingEnvironment.IsDevelopment()
                    ? "log4net.config"
                    : "log4net.Production.config")
            );
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseAbp(); //Initializes ABP framework.

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseStatusCodePagesWithRedirects("~/Error?statusCode={0}");
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<AbpCommonHub>("/signalr");

            endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

            app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>().EndpointConfiguration
                .ConfigureAllEndpoints(endpoints);
        });

        if (bool.Parse(_appConfiguration["HealthChecks:HealthChecksEnabled"]))
        {
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            if (bool.Parse(_appConfiguration["HealthChecks:HealthChecksUI:HealthChecksUIEnabled"]))
                app.UseHealthChecksUI();
        }
    }

    private void ConfigureKestrel(IServiceCollection services)
    {
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Listen(new IPEndPoint(IPAddress.Any, 443),
                listenOptions =>
                {
                    var certPassword = _appConfiguration.GetValue<string>("Kestrel:Certificates:Default:Password");
                    var certPath = _appConfiguration.GetValue<string>("Kestrel:Certificates:Default:Path");
                    var cert = new X509Certificate2(certPath, certPassword);
                    listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = cert
                    });
                });
        });
    }

    private void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddAbpZeroHealthCheck();

        var healthCheckUISection = _appConfiguration.GetSection("HealthChecks")?.GetSection("HealthChecksUI");

        if (bool.Parse(healthCheckUISection["HealthChecksUIEnabled"]))
        {
            services.Configure<Settings>(settings =>
            {
                healthCheckUISection.Bind(settings, c => c.BindNonPublicProperties = true);
            });
            services.AddHealthChecksUI()
                .AddInMemoryStorage();
        }
    }
}