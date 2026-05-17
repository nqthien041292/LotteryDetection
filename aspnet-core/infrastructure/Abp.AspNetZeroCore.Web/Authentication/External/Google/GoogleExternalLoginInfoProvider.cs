using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Google;

public class GoogleExternalLoginInfoProvider : IExternalLoginInfoProvider
{
    public GoogleExternalLoginInfoProvider(
        string clientId,
        string clientSecret,
        string userInfoEndpoint)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        UserInfoEndpoint = userInfoEndpoint;
        CreateExternalLoginInfo();
    }

    protected string ClientId { get; set; }

    protected string ClientSecret { get; set; }

    protected string UserInfoEndpoint { get; set; }

    protected ExternalLoginProviderInfo ExternalLoginProviderInfo { get; set; }
    public string Name { get; } = "Google";

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        return ExternalLoginProviderInfo;
    }

    private void CreateExternalLoginInfo()
    {
        ExternalLoginProviderInfo = new ExternalLoginProviderInfo("Google", ClientId, ClientSecret,
            typeof(GoogleAuthProviderApi), new Dictionary<string, string>
            {
                {
                    "UserInfoEndpoint",
                    UserInfoEndpoint
                }
            });
    }
}