using Abp.Dependency;

namespace LotteryDetection.MultiTenancy.Payments;

public interface IPaymentGatewayConfiguration : ITransientDependency
{
    bool IsActive { get; }

    bool SupportsRecurringPayments { get; }

    SubscriptionPaymentGatewayType GatewayType { get; }
}

