using System;

namespace LotteryDetection.Authorization.PasswordlessLogin;

public class PasswordlessLoginCodeCacheItem
{
    public const string CacheName = "AppPasswordlessVerificationCodeCache";

    public static readonly TimeSpan DefaultSlidingExpireTime = TimeSpan.FromMinutes(1);

    public PasswordlessLoginCodeCacheItem()
    {
    }

    public PasswordlessLoginCodeCacheItem(string code)
    {
        Code = code;
    }

    public string Code { get; set; }
}