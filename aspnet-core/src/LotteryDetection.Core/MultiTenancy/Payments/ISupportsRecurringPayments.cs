using Abp.Events.Bus.Handlers;
using LotteryDetection.MultiTenancy.Subscription;

namespace LotteryDetection.MultiTenancy.Payments;

public interface ISupportsRecurringPayments :
    IEventHandler<RecurringPaymentsDisabledEventData>,
    IEventHandler<RecurringPaymentsEnabledEventData>,
    IEventHandler<SubscriptionUpdatedEventData>,
    IEventHandler<SubscriptionCancelledEventData>
{

}

