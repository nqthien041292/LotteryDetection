using System;
using Abp.AspNetZeroCore;
using Abp.AspNetZeroCore.Timing;
using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.MailKit;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.Net.Mail.Smtp;
using Abp.OpenIddict;
using Abp.Reflection.Extensions;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using Abp.Zero.Ldap;
using Castle.MicroKernel.Registration;
using LotteryDetection.Authentication.TwoFactor;
using LotteryDetection.Authorization.Delegation;
using LotteryDetection.Authorization.Impersonation;
using LotteryDetection.Authorization.PasswordlessLogin;
using LotteryDetection.Authorization.QrLogin;
using LotteryDetection.Authorization.Roles;
using LotteryDetection.Authorization.Users;
using LotteryDetection.Chat;
using LotteryDetection.Configuration;
using LotteryDetection.DashboardCustomization.Definitions;
using LotteryDetection.Debugging;
using LotteryDetection.DynamicEntityProperties;
using LotteryDetection.Features;
using LotteryDetection.Friendships;
using LotteryDetection.Friendships.Cache;
using LotteryDetection.Localization;
using LotteryDetection.MultiTenancy;
using LotteryDetection.Net.Emailing;
using LotteryDetection.Notifications;
using LotteryDetection.WebHooks;
using MailKit.Security;

namespace LotteryDetection;

[DependsOn(
    typeof(LotteryDetectionCoreSharedModule),
    typeof(AbpZeroCoreModule),
    typeof(AbpZeroLdapModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetZeroCoreModule),
    typeof(AbpMailKitModule),
    typeof(AbpZeroCoreOpenIddictModule))]
public class LotteryDetectionCoreModule : AbpModule
{
    public override void PreInitialize()
    {
        //workaround for issue: https://github.com/aspnet/EntityFrameworkCore/issues/9825
        //related github issue: https://github.com/aspnet/EntityFrameworkCore/issues/10407
        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue9825", true);

        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        //Declare entity types
        Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
        Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
        Configuration.Modules.Zero().EntityTypes.User = typeof(User);

        LotteryDetectionLocalizationConfigurer.Configure(Configuration.Localization);

        //Adding feature providers
        Configuration.Features.Providers.Add<AppFeatureProvider>();

        //Adding setting providers
        Configuration.Settings.Providers.Add<AppSettingProvider>();

        //Adding notification providers
        Configuration.Notifications.Providers.Add<AppNotificationProvider>();

        //Adding webhook definition providers
        Configuration.Webhooks.Providers.Add<AppWebhookDefinitionProvider>();
        Configuration.Webhooks.TimeoutDuration = TimeSpan.FromMinutes(1);
        Configuration.Webhooks.IsAutomaticSubscriptionDeactivationEnabled = false;

        //Enable this line to create a multi-tenant application.
        Configuration.MultiTenancy.IsEnabled = LotteryDetectionConsts.MultiTenancyEnabled;

        //Enable LDAP authentication
        //Configuration.Modules.ZeroLdap().Enable(typeof(AppLdapAuthenticationSource));

        //Twilio - Enable this line to activate Twilio SMS integration
        //Configuration.ReplaceService<ISmsSender,TwilioSmsSender>();

        //Adding DynamicEntityParameters definition providers
        Configuration.DynamicEntityProperties.Providers.Add<AppDynamicEntityPropertyDefinitionProvider>();

        // MailKit configuration
        Configuration.Modules.AbpMailKit().SecureSocketOption = SecureSocketOptions.Auto;
        Configuration.ReplaceService<IMailKitSmtpBuilder, LotteryDetectionMailKitSmtpBuilder>(DependencyLifeStyle
            .Transient);

        //Configure roles
        AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

        if (DebugHelper.IsDebug)
            //Disabling email sending in debug mode
            Configuration.ReplaceService<IEmailSender, NullEmailSender>(DependencyLifeStyle.Transient);

        Configuration.ReplaceService(typeof(IEmailSenderConfiguration), () =>
        {
            Configuration.IocManager.IocContainer.Register(
                Component.For<IEmailSenderConfiguration, ISmtpEmailSenderConfiguration>()
                    .ImplementedBy<LotteryDetectionSmtpEmailSenderConfiguration>()
                    .LifestyleTransient()
            );
        });

        // Configures caching with sliding expiration times for  cache items.
        ConfigureCaching();

        IocManager.Register<DashboardConfiguration>();

        Configuration.Notifications.Notifiers.Add<SmsRealTimeNotifier>();
        Configuration.Notifications.Notifiers.Add<EmailRealTimeNotifier>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionCoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.RegisterIfNot<IChatCommunicator, NullChatCommunicator>();
        IocManager.Register<IUserDelegationConfiguration, UserDelegationConfiguration>();

        IocManager.Resolve<ChatUserStateWatcher>().Initialize();
        IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
    }

    private void ConfigureCaching()
    {
        Configuration.Caching.Configure(FriendCacheItem.CacheName,
            cache => { cache.DefaultSlidingExpireTime = FriendCacheItem.DefaultSlidingExpireTime; });

        Configuration.Caching.Configure(TwoFactorCodeCacheItem.CacheName,
            cache => { cache.DefaultSlidingExpireTime = TwoFactorCodeCacheItem.DefaultSlidingExpireTime; });

        Configuration.Caching.Configure(PasswordlessLoginCodeCacheItem.CacheName,
            cache => { cache.DefaultSlidingExpireTime = PasswordlessLoginCodeCacheItem.DefaultSlidingExpireTime; });

        Configuration.Caching.Configure(ImpersonationCacheItem.CacheName,
            cache => { cache.DefaultSlidingExpireTime = ImpersonationCacheItem.DefaultSlidingExpireTime; });

        Configuration.Caching.Configure(SwitchToLinkedAccountCacheItem.CacheName,
            cache => { cache.DefaultSlidingExpireTime = SwitchToLinkedAccountCacheItem.DefaultSlidingExpireTime; });

        Configuration.Caching.Configure(QrLoginSessionIdCacheItem.CacheName,
            cache => { cache.DefaultSlidingExpireTime = QrLoginSessionIdCacheItem.DefaultSlidingExpireTime; });
    }
}