using System;
using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public class ExternalLoginProviderInfo
{
    public ExternalLoginProviderInfo(
        string name,
        string clientId,
        string clientSecret,
        Type providerApiType,
        Dictionary<string, string> additionalParams = null,
        List<JsonClaimMap> claimMappings = null)
    {
        Name = name;
        ClientId = clientId;
        ClientSecret = clientSecret;
        ProviderApiType = providerApiType;
        AdditionalParams = additionalParams ?? new Dictionary<string, string>();
        ClaimMappings = claimMappings ?? new List<JsonClaimMap>();
    }

    public string Name { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public Type ProviderApiType { get; set; }

    public Dictionary<string, string> AdditionalParams { get; set; }

    public List<JsonClaimMap> ClaimMappings { get; set; }
}