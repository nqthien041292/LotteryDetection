using System;
using System.Net;
using System.Text;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterRequestBase
{
    public string ConsumerKey { get; set; }

    public virtual string GetParameterString(string parameterSeparator)
    {
        var totalSeconds = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes(totalSeconds.ToString()));
        return "oauth_consumer_key=" + WebUtility.UrlEncode(ConsumerKey) + parameterSeparator + "oauth_nonce=" +
               WebUtility.UrlEncode(base64String) + parameterSeparator + "oauth_signature_method=" +
               WebUtility.UrlEncode("HMAC-SHA1") + parameterSeparator + "oauth_timestamp=" +
               WebUtility.UrlEncode(totalSeconds.ToString()) + parameterSeparator + "oauth_version=" +
               WebUtility.UrlEncode("1.0");
    }
}