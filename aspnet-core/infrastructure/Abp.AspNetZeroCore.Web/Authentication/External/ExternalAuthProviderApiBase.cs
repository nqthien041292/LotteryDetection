using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Dependency;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public abstract class ExternalAuthProviderApiBase : IExternalAuthProviderApi, ITransientDependency
{
    public ExternalLoginProviderInfo ProviderInfo { get; set; }

    public void Initialize(ExternalLoginProviderInfo providerInfo)
    {
        ProviderInfo = providerInfo;
    }

    public async Task<bool> IsValidUser(string userId, string accessCode)
    {
        return (await GetUserInfo(accessCode)).ProviderKey == userId;
    }

    public abstract Task<ExternalAuthUserInfo> GetUserInfo(string accessCode);

    protected virtual void FillClaimsFromJObject(ExternalAuthUserInfo userInfo, JObject payload)
    {
        var claimKeyValueList = new List<ClaimKeyValue>();
        foreach (var keyValuePair in payload)
            claimKeyValueList.Add(new ClaimKeyValue(keyValuePair.Key, keyValuePair.Value?.ToString()));
        userInfo.Claims = claimKeyValueList;
    }
}