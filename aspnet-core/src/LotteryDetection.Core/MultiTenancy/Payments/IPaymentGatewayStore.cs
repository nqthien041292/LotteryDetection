using System.Collections.Generic;

namespace LotteryDetection.MultiTenancy.Payments;

public interface IPaymentGatewayStore
{
    List<PaymentGatewayModel> GetActiveGateways();
}

