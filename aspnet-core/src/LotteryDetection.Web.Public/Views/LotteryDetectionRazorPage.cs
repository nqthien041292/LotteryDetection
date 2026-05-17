using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace LotteryDetection.Web.Public.Views;

public abstract class LotteryDetectionRazorPage<TModel> : AbpRazorPage<TModel>
{
    protected LotteryDetectionRazorPage()
    {
        LocalizationSourceName = LotteryDetectionConsts.LocalizationSourceName;
    }

    [RazorInject] public IAbpSession AbpSession { get; set; }
}