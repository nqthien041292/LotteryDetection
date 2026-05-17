using Abp.Runtime.Caching;
using LotteryDetection.Authentication.TwoFactor;

namespace LotteryDetection.Web.Authentication.TwoFactor;

public static class TwoFactorCodeCacheExtensions
{
    public static ITypedCache<string, TwoFactorCodeCacheItem> GetTwoFactorCodeCache(this ICacheManager cacheManager)
    {
        return cacheManager.GetCache<string, TwoFactorCodeCacheItem>(TwoFactorCodeCacheItem.CacheName);
    }
}

