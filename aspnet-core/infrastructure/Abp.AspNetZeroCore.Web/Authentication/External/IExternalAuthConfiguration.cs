using System;
using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public interface IExternalAuthConfiguration
{
    [Obsolete("Use IExternalLoginInfoProviders")]
    List<ExternalLoginProviderInfo> Providers { get; }

    List<IExternalLoginInfoProvider> ExternalLoginInfoProviders { get; }
}