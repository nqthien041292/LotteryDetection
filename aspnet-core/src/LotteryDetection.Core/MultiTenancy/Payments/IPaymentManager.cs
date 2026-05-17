using System.Threading.Tasks;
using Abp.Domain.Services;

namespace LotteryDetection.MultiTenancy.Payments;

public interface IPaymentManager : IDomainService
{
    Task<long> CreatePayment(SubscriptionPayment payment);

    Task<bool> HasAnyPayment(int tenantId);

    SubscriptionPayment GetLastCompletedSubscriptionPayment(int tenantId);

    Task<SubscriptionPayment> GetPayment(long paymentId);

    Task UpdatePayment(SubscriptionPayment payment);
}