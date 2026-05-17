using System.Text.RegularExpressions;

namespace LotteryDetection.Web.Xss;

public class DefaultHtmlSanitizer : IHtmlSanitizer
{
    public string Sanitize(string html)
    {
        return Regex.Replace(html, "<.*?>|&.*?;", string.Empty);
    }
}