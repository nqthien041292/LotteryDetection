using Abp.AspNetCore.Mvc.Views;

namespace LotteryDetection.Web.Views;

public abstract class LotteryDetectionRazorPage<TModel> : AbpRazorPage<TModel>
{
    protected LotteryDetectionRazorPage()
    {
        LocalizationSourceName = LotteryDetectionConsts.LocalizationSourceName;
    }
}