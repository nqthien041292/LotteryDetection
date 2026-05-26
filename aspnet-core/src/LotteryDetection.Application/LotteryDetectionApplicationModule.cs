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
        
        IocManager.Register<LotteryDetection.Lottery.ILotteryResultProvider, LotteryDetection.Lottery.Scraping.MinhNgocResultProvider>(Abp.Dependency.DependencyLifeStyle.Transient);
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionApplicationModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        var workManager = IocManager.Resolve<Abp.Threading.BackgroundWorkers.IBackgroundWorkerManager>();
        workManager.Add(IocManager.Resolve<LotteryDetection.Lottery.Workers.LotteryResultWorker>());
    }
}