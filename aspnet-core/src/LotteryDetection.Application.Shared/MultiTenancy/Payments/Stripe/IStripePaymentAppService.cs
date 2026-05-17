using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.MultiTenancy.Payments.Dto;
using LotteryDetection.MultiTenancy.Payments.Stripe.Dto;

namespace LotteryDetection.MultiTenancy.Payments.Stripe;

public interface IStripePaymentAppService : IApplicationService
{
    Task ConfirmPayment(StripeConfirmPaymentInput input);

    StripeConfigurationDto GetConfiguration();

    Task<string> CreatePaymentSession(StripeCreatePaymentSessionInput input);
}

