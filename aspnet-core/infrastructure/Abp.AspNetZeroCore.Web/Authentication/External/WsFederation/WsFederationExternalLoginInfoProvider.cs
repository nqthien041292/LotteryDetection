using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External.WsFederation;

public class WsFederationExternalLoginInfoProvider : IExternalLoginInfoProvider
{
    public WsFederationExternalLoginInfoProvider(
        string clientId,
        string tenant,
        string metaDataAddress,
        string authority,
        List<JsonClaimMap> jsonClaimMaps = null)
    {
        ClientId = clientId;
        Tenant = tenant;
        MetaDataAddress = metaDataAddress;
        Authority = authority;
        JsonClaimMaps = jsonClaimMaps;
        CreateExternalLoginProviderInfo();
    }

    protected string ClientId { get; set; }

    protected string Tenant { get; set; }

    protected string MetaDataAddress { get; set; }

    protected string Authority { get; set; }

    protected List<JsonClaimMap> JsonClaimMaps { get; set; }

    protected ExternalLoginProviderInfo ExternalLoginProviderInfo { get; set; }
    public string Name { get; } = "WsFederation";

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        return ExternalLoginProviderInfo;
    }

    private void CreateExternalLoginProviderInfo()
    {
        var clientId = ClientId;
        var providerApiType = typeof(WsFederationAuthProviderApi);
        var additionalParams = new Dictionary<string, string>
        {
            { "Tenant", Tenant },
            { "MetaDataAddress", MetaDataAddress },
            { "Authority", Authority }
        };
        var jsonClaimMaps = JsonClaimMaps;
        ExternalLoginProviderInfo = new ExternalLoginProviderInfo("WsFederation", clientId, "", providerApiType,
            additionalParams, jsonClaimMaps);
    }
}