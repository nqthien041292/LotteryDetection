using Abp.Dependency;
using LotteryDetection.MultiTenancy.Payments;
using LotteryDetection.Url;

namespace LotteryDetection.Web.Url;

public class PaymentUrlGenerator : IPaymentUrlGenerator, ITransientDependency
{
    private readonly IWebUrlService _webUrlService;

    public PaymentUrlGenerator(IWebUrlService webUrlService)
    {
        _webUrlService = webUrlService;
    }

    public string CreatePaymentRequestUrl(SubscriptionPayment subscriptionPayment)
    {
        var webSiteRootAddress = _webUrlService.GetSiteRootAddress();

        return webSiteRootAddress +
               "account/gateway-selection?paymentId=" +
               subscriptionPayment.Id;
    }
}

