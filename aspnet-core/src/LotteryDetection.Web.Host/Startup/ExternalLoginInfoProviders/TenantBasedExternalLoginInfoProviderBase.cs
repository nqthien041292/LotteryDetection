using Abp.AspNetZeroCore.Web.Authentication.External;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;

namespace LotteryDetection.Web.Startup.ExternalLoginInfoProviders;

public abstract class TenantBasedExternalLoginInfoProviderBase : IExternalLoginInfoProvider
{
    private readonly IAbpSession _abpSession;
    private readonly ICacheManager _cacheManager;

    protected TenantBasedExternalLoginInfoProviderBase(
        IAbpSession abpSession,
        ICacheManager cacheManager)
    {
        _abpSession = abpSession;
        _cacheManager = cacheManager;
    }

    public abstract string Name { get; }

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        if (_abpSession.TenantId.HasValue && TenantHasSettings())
            return _cacheManager.GetExternalLoginInfoProviderCache()
                .Get(GetCacheKey(), GetTenantInformation);

        return _cacheManager.GetExternalLoginInfoProviderCache()
            .Get(GetCacheKey(), GetHostInformation);
    }

    protected abstract bool TenantHasSettings();

    protected abstract ExternalLoginProviderInfo GetTenantInformation();

    protected abstract ExternalLoginProviderInfo GetHostInformation();

    private string GetCacheKey()
    {
        if (_abpSession.TenantId.HasValue) return $"{Name}-{_abpSession.TenantId.Value}";

        return $"{Name}";
    }
}