using System;

namespace LotteryDetection.Authorization.QrLogin;

public class QrLoginSessionIdCacheItem
{
    public const string CacheName = "AppQrLoginSessionIdCache";

    public static readonly TimeSpan DefaultSlidingExpireTime = TimeSpan.FromMinutes(1);

    public QrLoginSessionIdCacheItem(string code)
    {
        Code = code;
    }

    public string Code { get; set; }
}