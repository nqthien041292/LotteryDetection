using Abp.Domain.Services;

namespace LotteryDetection;

public abstract class LotteryDetectionDomainServiceBase : DomainService
{
    /* Add your common members for all your domain services. */

    protected LotteryDetectionDomainServiceBase()
    {
        LocalizationSourceName = LotteryDetectionConsts.LocalizationSourceName;
    }
}

