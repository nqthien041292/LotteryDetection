using System.Threading.Tasks;
using LotteryDetection.MultiTenancy.Payments.Paypal;
using LotteryDetection.MultiTenancy.Payments.PayPal;
using LotteryDetection.MultiTenancy.Payments.PayPal.Dto;

namespace LotteryDetection.MultiTenancy.Payments;

public class PayPalPaymentAppService : LotteryDetectionAppServiceBase, IPayPalPaymentAppService
{
    private readonly PayPalGatewayManager _payPalGatewayManager;
    private readonly PayPalPaymentGatewayConfiguration _payPalPaymentGatewayConfiguration;
    private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;

    public PayPalPaymentAppService(
        PayPalGatewayManager payPalGatewayManager,
        ISubscriptionPaymentRepository subscriptionPaymentRepository,
        PayPalPaymentGatewayConfiguration payPalPaymentGatewayConfiguration)
    {
        _payPalGatewayManager = payPalGatewayManager;
        _subscriptionPaymentRepository = subscriptionPaymentRepository;
        _payPalPaymentGatewayConfiguration = payPalPaymentGatewayConfiguration;
    }

    public async Task ConfirmPayment(long paymentId, string paypalOrderId)
    {
        var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);

        await _payPalGatewayManager.CaptureOrderAsync(
            new PayPalCaptureOrderRequestInput(paypalOrderId)
        );

        payment.Gateway = SubscriptionPaymentGatewayType.Paypal;
        payment.ExternalPaymentId = paypalOrderId;
        payment.SetAsPaid();
    }

    public PayPalConfigurationDto GetConfiguration()
    {
        return new PayPalConfigurationDto
        {
            ClientId = _payPalPaymentGatewayConfiguration.ClientId,
            DemoUsername = _payPalPaymentGatewayConfiguration.DemoUsername,
            DemoPassword = _payPalPaymentGatewayConfiguration.DemoPassword,
            DisabledFundings = _payPalPaymentGatewayConfiguration.DisabledFundings
        };
    }
}