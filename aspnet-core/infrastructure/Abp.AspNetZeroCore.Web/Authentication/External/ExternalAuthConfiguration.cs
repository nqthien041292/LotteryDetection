using System;
using System.Collections.Generic;
using Abp.Dependency;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public class ExternalAuthConfiguration : IExternalAuthConfiguration, ISingletonDependency
{
    [Obsolete]
    public ExternalAuthConfiguration()
    {
        Providers = new List<ExternalLoginProviderInfo>();
        ExternalLoginInfoProviders = new List<IExternalLoginInfoProvider>();
    }

    [Obsolete("Use IExternalLoginInfoProviders")]
    public List<ExternalLoginProviderInfo> Providers { get; }

    public List<IExternalLoginInfoProvider> ExternalLoginInfoProviders { get; }
}