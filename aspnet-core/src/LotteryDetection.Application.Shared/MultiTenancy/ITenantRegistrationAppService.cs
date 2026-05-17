using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.Editions.Dto;
using LotteryDetection.MultiTenancy.Dto;

namespace LotteryDetection.MultiTenancy;

public interface ITenantRegistrationAppService : IApplicationService
{
    Task<RegisterTenantOutput> RegisterTenant(RegisterTenantInput input);

    Task<EditionsSelectOutput> GetEditionsForSelect();

    Task<EditionSelectDto> GetEdition(int editionId);

    Task BuyNowSucceed(long paymentId);

    Task NewRegistrationSucceed(long paymentId);

    Task UpgradeSucceed(long paymentId);

    Task ExtendSucceed(long paymentId);
}