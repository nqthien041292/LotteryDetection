using Abp.Modules;
using Abp.Reflection.Extensions;

namespace LotteryDetection;

public class LotteryDetectionCoreSharedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionCoreSharedModule).GetAssembly());
    }
}