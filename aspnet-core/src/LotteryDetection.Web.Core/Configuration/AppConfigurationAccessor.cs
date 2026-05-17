using Abp.Dependency;
using LotteryDetection.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Web.Configuration;

public class AppConfigurationAccessor : IAppConfigurationAccessor, ISingletonDependency
{
    public AppConfigurationAccessor(IWebHostEnvironment env)
    {
        Configuration = env.GetAppConfiguration();
    }

    public IConfigurationRoot Configuration { get; }
}