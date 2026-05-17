using System.IO;
using Abp.Dependency;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Configuration;

/* This service is replaced in Web layer and Test project separately */
public class DefaultAppConfigurationAccessor : IAppConfigurationAccessor, ISingletonDependency
{
    public DefaultAppConfigurationAccessor()
    {
        Configuration = AppConfigurations.Get(Directory.GetCurrentDirectory());
    }

    public IConfigurationRoot Configuration { get; }
}