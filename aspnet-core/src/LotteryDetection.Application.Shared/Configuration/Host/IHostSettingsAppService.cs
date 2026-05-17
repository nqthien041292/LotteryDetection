using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.Configuration.Host.Dto;

namespace LotteryDetection.Configuration.Host;

public interface IHostSettingsAppService : IApplicationService
{
    Task<HostSettingsEditDto> GetAllSettings();

    Task UpdateAllSettings(HostSettingsEditDto input);

    Task SendTestEmail(SendTestEmailInput input);
}