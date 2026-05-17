using Abp.Dependency;
using Castle.Core.Logging;

namespace LotteryDetection.Configuration;

/* This service is replaced in Web layer */
public class DefaultAppConfigurationWriter : IAppConfigurationWriter, ISingletonDependency
{
    public DefaultAppConfigurationWriter()
    {
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    public void Write(string key, string value)
    {
        Logger.Warn("Write is not implemented!");
    }
}