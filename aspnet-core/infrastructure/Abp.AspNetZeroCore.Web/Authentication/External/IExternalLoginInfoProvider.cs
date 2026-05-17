namespace Abp.AspNetZeroCore.Web.Authentication.External;

public interface IExternalLoginInfoProvider
{
    string Name { get; }

    ExternalLoginProviderInfo GetExternalLoginInfo();
}