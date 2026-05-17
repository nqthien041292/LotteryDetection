using System.Net;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterGetTokenRequest : TwitterRequestBase
{
    public string CallbackUrl { get; set; }

    public override string GetParameterString(string parameterSeparator)
    {
        var parameterString = base.GetParameterString(parameterSeparator);
        return "oauth_callback=" + WebUtility.UrlEncode(CallbackUrl) + parameterSeparator + parameterString;
    }
}