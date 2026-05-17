using Abp.AspNetZeroCore.Web.Authentication.External;
using Abp.AspNetZeroCore.Web.Authentication.External.Facebook;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Json;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using LotteryDetection.Authentication;
using LotteryDetection.Configuration;

namespace LotteryDetection.Web.Startup.ExternalLoginInfoProviders;

public class TenantBasedFacebookExternalLoginInfoProvider : TenantBasedExternalLoginInfoProviderBase,
    ISingletonDependency
{
    private readonly IAbpSession _abpSession;
    private readonly ISettingManager _settingManager;

    public TenantBasedFacebookExternalLoginInfoProvider(
        ISettingManager settingManager,
        IAbpSession abpSession,
        ICacheManager cacheManager) : base(abpSession, cacheManager)
    {
        _settingManager = settingManager;
        _abpSession = abpSession;
    }

    public override string Name { get; } = FacebookAuthProviderApi.Name;

    private ExternalLoginProviderInfo CreateExternalLoginInfo(FacebookExternalLoginProviderSettings settings)
    {
        return new ExternalLoginProviderInfo(Name, settings.AppId, settings.AppSecret, typeof(FacebookAuthProviderApi));
    }

    protected override bool TenantHasSettings()
    {
        var settingValue = _settingManager.GetSettingValueForTenant(AppSettings.ExternalLoginProvider.Tenant.Facebook,
            _abpSession.TenantId.Value);
        return !settingValue.IsNullOrWhiteSpace();
    }

    protected override ExternalLoginProviderInfo GetTenantInformation()
    {
        var settingValue = _settingManager.GetSettingValueForTenant(AppSettings.ExternalLoginProvider.Tenant.Facebook,
            _abpSession.TenantId.Value);
        var settings = settingValue.FromJsonString<FacebookExternalLoginProviderSettings>();
        return CreateExternalLoginInfo(settings);
    }

    protected override ExternalLoginProviderInfo GetHostInformation()
    {
        var settingValue =
            _settingManager.GetSettingValueForApplication(AppSettings.ExternalLoginProvider.Host.Facebook);
        var settings = settingValue.FromJsonString<FacebookExternalLoginProviderSettings>();
        return CreateExternalLoginInfo(settings);
    }
}