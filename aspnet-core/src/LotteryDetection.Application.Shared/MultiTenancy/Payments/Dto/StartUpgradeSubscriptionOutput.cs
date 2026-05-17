using System;

namespace LotteryDetection.MultiTenancy.Payments.Dto;

public class StartUpgradeSubscriptionOutput
{
    public StartUpgradeSubscriptionOutput(bool upgraded, long? paymentId = null)
    {
        if (!upgraded && !paymentId.HasValue)
            throw new ArgumentException("paymentId can not be null if upgraded is false!");

        Upgraded = upgraded;
        PaymentId = paymentId;
    }

    public long? PaymentId { get; set; }

    public bool Upgraded { get; set; }
}