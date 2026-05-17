using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace LotteryDetection.Web.Public.Views;

public abstract class LotteryDetectionRazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected LotteryDetectionRazorPage()
    {
        LocalizationSourceName = LotteryDetectionConsts.LocalizationSourceName;
    }
}

