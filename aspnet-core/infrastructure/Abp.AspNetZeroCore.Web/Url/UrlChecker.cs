using System.Text.RegularExpressions;

namespace Abp.AspNetZeroCore.Web.Url;

public static class UrlChecker
{
    private static readonly Regex UrlWithProtocolRegex = new("^.{1,10}://.*$");

    public static bool IsRooted(string url)
    {
        return url.StartsWith("/") || UrlWithProtocolRegex.IsMatch(url);
    }
}