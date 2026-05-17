using System;

namespace LotteryDetection.Authorization.Users.Profile.Cache;

[Serializable]
public class SmsVerificationCodeCacheItem
{
    public const string CacheName = "AppSmsVerificationCodeCache";

    public SmsVerificationCodeCacheItem()
    {
    }

    public SmsVerificationCodeCacheItem(string code)
    {
        Code = code;
    }

    public string Code { get; set; }
}