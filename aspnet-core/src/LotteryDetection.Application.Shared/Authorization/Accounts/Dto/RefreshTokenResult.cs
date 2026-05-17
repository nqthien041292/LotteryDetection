namespace LotteryDetection.Authorization.Accounts.Dto;

public class RefreshTokenResult
{
    public RefreshTokenResult(string accessToken, string encryptedAccessToken, int expireInSeconds)
    {
        AccessToken = accessToken;
        ExpireInSeconds = expireInSeconds;
        EncryptedAccessToken = encryptedAccessToken;
    }

    public RefreshTokenResult()
    {
    }

    public string AccessToken { get; set; }

    public string EncryptedAccessToken { get; set; }

    public int ExpireInSeconds { get; set; }
}