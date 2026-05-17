using Abp.Modules;
using Abp.Reflection.Extensions;

namespace LotteryDetection;

[DependsOn(typeof(LotteryDetectionCoreSharedModule))]
public class LotteryDetectionApplicationSharedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionApplicationSharedModule).GetAssembly());
    }
}