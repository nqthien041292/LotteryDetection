using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterSignatureHelper
{
    public string GetBase64Signature(
        string requestUrl,
        string httpMethod,
        string parameterString,
        string twitterSecret)
    {
        var s1 = httpMethod + "&" + WebUtility.UrlEncode(requestUrl) + "&" + WebUtility.UrlEncode(parameterString);
        var s2 = WebUtility.UrlEncode(twitterSecret) + "&";
        var bytes = Encoding.ASCII.GetBytes(s1);
        return Convert.ToBase64String(new HMACSHA1(Encoding.ASCII.GetBytes(s2)).ComputeHash(bytes));
    }
}