using System.Collections.Generic;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Microsoft;

public class MicrosoftExternalLoginInfoProvider : IExternalLoginInfoProvider
{
    public MicrosoftExternalLoginInfoProvider(
        string consumerKey,
        string consumerSecret,
        Dictionary<string, string> additionalParameters = null)
    {
        ConsumerKey = consumerKey;
        ConsumerSecret = consumerSecret;
        AdditionalParameters = additionalParameters;
        CreateExternalLoginInfo();
    }

    protected string ConsumerKey { get; set; }

    protected string ConsumerSecret { get; set; }

    protected Dictionary<string, string> AdditionalParameters { get; set; }

    protected ExternalLoginProviderInfo ExternalLoginProviderInfo { get; set; }
    public string Name { get; } = "Microsoft";

    public virtual ExternalLoginProviderInfo GetExternalLoginInfo()
    {
        return ExternalLoginProviderInfo;
    }

    private void CreateExternalLoginInfo()
    {
        ExternalLoginProviderInfo = new ExternalLoginProviderInfo("Microsoft", ConsumerKey, ConsumerSecret,
            typeof(MicrosoftAuthProviderApi), AdditionalParameters);
    }
}