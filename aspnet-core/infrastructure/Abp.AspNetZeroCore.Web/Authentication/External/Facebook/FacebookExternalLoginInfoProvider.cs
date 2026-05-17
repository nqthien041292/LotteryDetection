namespace Abp.AspNetZeroCore.Web.Authentication.External.Facebook;

public class FacebookExternalLoginInfoProvider : IExternalLoginInfoProvider
{
    public FacebookExternalLoginInfoProvider(string appId, string appSecret)
    {
        AppId = appId;
        AppSecret = appSecret;
        CreateExternalLoginInfo();
    }

    protected string AppId { get; set; }

    protected string AppSecret { get; set; }

    protected ExternalLoginProviderInfo ExternalLoginProviderInfo { get; set; }
    public string Name { get; } = "Facebook";

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        return ExternalLoginProviderInfo;
    }

    private void CreateExternalLoginInfo()
    {
        ExternalLoginProviderInfo =
            new ExternalLoginProviderInfo("Facebook", AppId, AppSecret, typeof(FacebookAuthProviderApi));
    }
}