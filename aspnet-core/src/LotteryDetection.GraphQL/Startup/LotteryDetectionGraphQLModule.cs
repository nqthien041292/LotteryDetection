using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace LotteryDetection.Startup;

[DependsOn(typeof(LotteryDetectionCoreModule))]
public class LotteryDetectionGraphQLModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionGraphQLModule).GetAssembly());
    }

    public override void PreInitialize()
    {
        base.PreInitialize();

        //Adding custom AutoMapper configuration
        Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
    }
}

