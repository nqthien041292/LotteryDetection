using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public class ExternalAuthUserInfo
{
    public string ProviderKey { get; set; }

    public string Name { get; set; }

    public string EmailAddress { get; set; }

    public string Surname { get; set; }

    public string Provider { get; set; }

    public List<ClaimKeyValue> Claims { get; internal set; } = new();
}