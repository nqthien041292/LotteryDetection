using System;

namespace LotteryDetection.Authentication.TwoFactor;

[Serializable]
public class TwoFactorCodeCacheItem
{
    public const string CacheName = "AppTwoFactorCodeCache";

    public static readonly TimeSpan DefaultSlidingExpireTime = TimeSpan.FromMinutes(2);

    public TwoFactorCodeCacheItem()
    {
    }

    public TwoFactorCodeCacheItem(string code)
    {
        Code = code;
    }

    public string Code { get; set; }
}