using LotteryDetection.MultiTenancy.Payments.Stripe;

namespace LotteryDetection.Web.Controllers;

public class StripeController : StripeControllerBase
{
    public StripeController(
        StripeGatewayManager stripeGatewayManager,
        StripePaymentGatewayConfiguration stripeConfiguration,
        IStripePaymentAppService stripePaymentAppService)
        : base(stripeGatewayManager, stripeConfiguration, stripePaymentAppService)
    {
    }
}