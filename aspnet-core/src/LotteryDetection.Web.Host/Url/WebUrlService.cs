using Abp.Dependency;
using LotteryDetection.Configuration;
using LotteryDetection.Url;

namespace LotteryDetection.Web.Url;

public class WebUrlService : WebUrlServiceBase, IWebUrlService, ITransientDependency
{
    public WebUrlService(
        IAppConfigurationAccessor configurationAccessor) :
        base(configurationAccessor)
    {
    }

    public override string WebSiteRootAddressFormatKey => "App:ClientRootAddress";

    public override string ServerRootAddressFormatKey => "App:ServerRootAddress";
}

