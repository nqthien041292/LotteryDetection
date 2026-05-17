using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using LotteryDetection.Configuration;
using LotteryDetection.EntityFrameworkCore;
using LotteryDetection.Migrator.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Migrator;

[DependsOn(typeof(LotteryDetectionEntityFrameworkCoreModule))]
public class LotteryDetectionMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public LotteryDetectionMigratorModule(
        LotteryDetectionEntityFrameworkCoreModule lotteryDetectionEntityFrameworkCoreModule)
    {
        lotteryDetectionEntityFrameworkCoreModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(LotteryDetectionMigratorModule).GetAssembly().GetDirectoryPathOrNull(),
            addUserSecrets: true
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            LotteryDetectionConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(typeof(IEventBus), () =>
        {
            IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            );
        });
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}