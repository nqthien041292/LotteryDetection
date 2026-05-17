using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using LotteryDetection.Authorization;

namespace LotteryDetection;

/// <summary>
///     Application layer module of the application.
/// </summary>
[DependsOn(
    typeof(LotteryDetectionApplicationSharedModule),
    typeof(LotteryDetectionCoreModule)
)]
public class LotteryDetectionApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        //Adding authorization providers
        Configuration.Authorization.Providers.Add<AppAuthorizationProvider>();

        //Adding custom AutoMapper configuration
        Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionApplicationModule).GetAssembly());
    }
}