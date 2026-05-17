using Abp.Dependency;
using Abp.Reflection.Extensions;
using Microsoft.Extensions.Configuration;
using LotteryDetection.Configuration;

namespace LotteryDetection.Test.Base
{
    public class TestAppConfigurationAccessor : IAppConfigurationAccessor, ISingletonDependency
    {
        public IConfigurationRoot Configuration { get; }

        public TestAppConfigurationAccessor()
        {
            Configuration = AppConfigurations.Get(
                typeof(LotteryDetectionTestBaseModule).GetAssembly().GetDirectoryPathOrNull()
            );
        }
    }
}
