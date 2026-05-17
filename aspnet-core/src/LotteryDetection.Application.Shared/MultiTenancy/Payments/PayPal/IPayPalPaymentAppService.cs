using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.MultiTenancy.Payments.PayPal.Dto;

namespace LotteryDetection.MultiTenancy.Payments.PayPal;

public interface IPayPalPaymentAppService : IApplicationService
{
    Task ConfirmPayment(long paymentId, string paypalOrderId);

    PayPalConfigurationDto GetConfiguration();
}