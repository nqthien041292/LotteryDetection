using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External.OpenIdConnect;

public class OpenIdConnectExternalLoginInfoProvider : IExternalLoginInfoProvider
{
    public OpenIdConnectExternalLoginInfoProvider(
        string clientId,
        string clientSecret,
        string authority,
        string loginUrl,
        bool validateIssuer,
        string responseType,
        List<JsonClaimMap> jsonClaimMaps)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        Authority = authority;
        LoginUrl = loginUrl;
        ValidateIssuer = validateIssuer;
        ResponseType = responseType;
        JsonClaimMaps = jsonClaimMaps;
        CreateExternalLoginProviderInfo();
    }

    protected string ClientId { get; set; }

    protected string ClientSecret { get; set; }

    protected string Authority { get; set; }

    protected string LoginUrl { get; set; }

    protected bool ValidateIssuer { get; set; }

    protected string ResponseType { get; set; }

    protected List<JsonClaimMap> JsonClaimMaps { get; set; }

    protected ExternalLoginProviderInfo ExternalLoginProviderInfo { get; set; }
    public string Name { get; } = "OpenIdConnect";

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        return ExternalLoginProviderInfo;
    }

    private void CreateExternalLoginProviderInfo()
    {
        var clientId = ClientId;
        var clientSecret = ClientSecret;
        var providerApiType = typeof(OpenIdConnectAuthProviderApi);
        var additionalParams = new Dictionary<string, string>
        {
            { "Authority", Authority },
            { "LoginUrl", LoginUrl },
            { "ValidateIssuer", ValidateIssuer.ToString() },
            { "ResponseType", ResponseType }
        };
        var jsonClaimMaps = JsonClaimMaps;
        ExternalLoginProviderInfo = new ExternalLoginProviderInfo("OpenIdConnect", clientId, clientSecret,
            providerApiType, additionalParams, jsonClaimMaps);
    }
}