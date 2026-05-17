using Abp.AspNetCore.Mvc.ViewComponents;

namespace LotteryDetection.Web.Public.Views;

public abstract class LotteryDetectionViewComponent : AbpViewComponent
{
    protected LotteryDetectionViewComponent()
    {
        LocalizationSourceName = LotteryDetectionConsts.LocalizationSourceName;
    }
}

