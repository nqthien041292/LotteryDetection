using System;
using System.IO;
using Abp;
using Abp.AspNetZeroCore;
using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.TestBase;
using Abp.Zero.Configuration;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;
using LotteryDetection.Authorization.Users;
using LotteryDetection.Configuration;
using LotteryDetection.EntityFrameworkCore;
using LotteryDetection.MultiTenancy;
using LotteryDetection.Security.Recaptcha;
using LotteryDetection.Test.Base.DependencyInjection;
using LotteryDetection.Test.Base.UiCustomization;
using LotteryDetection.Test.Base.Url;
using LotteryDetection.Test.Base.Web;
using LotteryDetection.UiCustomization;
using LotteryDetection.Url;
using NSubstitute;

namespace LotteryDetection.Test.Base
{
    [DependsOn(
        typeof(LotteryDetectionApplicationModule),
        typeof(LotteryDetectionEntityFrameworkCoreModule),
        typeof(AbpTestBaseModule))]
    public class LotteryDetectionTestBaseModule : AbpModule
    {
        public LotteryDetectionTestBaseModule(LotteryDetectionEntityFrameworkCoreModule lotteryDetectionEntityFrameworkCoreModule)
        {
            lotteryDetectionEntityFrameworkCoreModule.SkipDbContextRegistration = true;
        }

        public override void PreInitialize()
        {
            var configuration = GetConfiguration();

            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

            Configuration.UnitOfWork.Timeout = TimeSpan.FromMinutes(30);
            Configuration.UnitOfWork.IsTransactional = false;
            
            //Use database for language management
            Configuration.Modules.Zero().LanguageManagement.EnableDbLocalization();

            RegisterFakeService<AbpZeroDbMigrator>();

            IocManager.Register<IAppUrlService, FakeAppUrlService>();
            IocManager.Register<IWebUrlService, FakeWebUrlService>();
            IocManager.Register<IRecaptchaValidator, FakeRecaptchaValidator>();

            Configuration.ReplaceService<IAppConfigurationAccessor, TestAppConfigurationAccessor>();
            Configuration.ReplaceService<IEmailSender, NullEmailSender>(DependencyLifeStyle.Transient);
            Configuration.ReplaceService<IUiThemeCustomizerFactory, NullUiThemeCustomizerFactory>();

            //Uncomment below line to write change logs for the entities below:
            Configuration.EntityHistory.IsEnabled = true;
            Configuration.EntityHistory.Selectors.Add("LotteryDetectionEntities", typeof(User), typeof(Tenant));
        }

        public override void Initialize()
        {
            ServiceCollectionRegistrar.Register(IocManager);
        }

        private void RegisterFakeService<TService>()
            where TService : class
        {
            IocManager.IocContainer.Register(
                Component.For<TService>()
                    .UsingFactoryMethod(() => Substitute.For<TService>())
                    .LifestyleSingleton()
            );
        }

        private static IConfigurationRoot GetConfiguration()
        {
            return AppConfigurations.Get(Directory.GetCurrentDirectory(), addUserSecrets: true);
        }
    }
}
