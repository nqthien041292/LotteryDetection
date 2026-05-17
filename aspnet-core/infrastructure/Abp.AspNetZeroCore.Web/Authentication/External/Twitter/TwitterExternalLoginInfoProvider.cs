namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterExternalLoginInfoProvider : IExternalLoginInfoProvider
{
    public TwitterExternalLoginInfoProvider(string apiKey, string apiKeySecret, string callbackUrl)
    {
        ApiKey = apiKey;
        ApiKeySecret = apiKeySecret;
        CreateExternalLoginInfo();
    }

    protected string ApiKey { get; set; }

    protected string ApiKeySecret { get; set; }

    protected ExternalLoginProviderInfo ExternalLoginProviderInfo { get; set; }
    public string Name { get; } = "Twitter";

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        return ExternalLoginProviderInfo;
    }

    private void CreateExternalLoginInfo()
    {
        ExternalLoginProviderInfo =
            new ExternalLoginProviderInfo("Twitter", ApiKey, ApiKeySecret, typeof(TwitterAuthProviderApi));
    }
}