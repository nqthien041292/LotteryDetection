using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.Configuration.Tenants.Dto;

namespace LotteryDetection.Configuration.Tenants;

public interface ITenantSettingsAppService : IApplicationService
{
    Task<TenantSettingsEditDto> GetAllSettings();

    Task UpdateAllSettings(TenantSettingsEditDto input);

    Task ClearDarkLogo();

    Task ClearDarkLogoMinimal();

    Task ClearLightLogo();

    Task ClearLightLogoMinimal();

    Task ClearCustomCss();
}