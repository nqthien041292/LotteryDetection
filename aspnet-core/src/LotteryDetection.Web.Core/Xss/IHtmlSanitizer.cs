using Abp.Dependency;

namespace LotteryDetection.Web.Xss;

public interface IHtmlSanitizer : ITransientDependency
{
    string Sanitize(string html);
}