using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.MultiTenancy.Dto;
using LotteryDetection.MultiTenancy.Payments.Dto;

namespace LotteryDetection.MultiTenancy;

public interface ISubscriptionAppService : IApplicationService
{
    Task DisableRecurringPayments();

    Task EnableRecurringPayments();

    Task<long> StartExtendSubscription(StartExtendSubscriptionInput input);

    Task<StartUpgradeSubscriptionOutput> StartUpgradeSubscription(StartUpgradeSubscriptionInput input);

    Task<long> StartTrialToBuySubscription(StartTrialToBuySubscriptionInput input);
}