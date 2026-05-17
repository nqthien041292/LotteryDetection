using Abp.Events.Bus;

namespace LotteryDetection.MultiTenancy.Subscription;

public class SubscriptionCancelledEventData : EventData
{
    public long PaymentId { get; set; }

    public string ExternalPaymentId { get; set; }
}