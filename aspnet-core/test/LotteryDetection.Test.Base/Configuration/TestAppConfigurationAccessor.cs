using Abp.Dependency;
using Abp.Reflection.Extensions;
using LotteryDetection.Configuration;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Test.Base.Configuration;

public class TestAppConfigurationAccessor : IAppConfigurationAccessor, ISingletonDependency
{
    public TestAppConfigurationAccessor()
    {
        Configuration = AppConfigurations.Get(
            typeof(LotteryDetectionTestBaseModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public IConfigurationRoot Configuration { get; }
}