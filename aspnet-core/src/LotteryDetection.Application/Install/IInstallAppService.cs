using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.Install.Dto;

namespace LotteryDetection.Install;

public interface IInstallAppService : IApplicationService
{
    Task Setup(InstallDto input);

    AppSettingsJsonDto GetAppSettingsJson();

    CheckDatabaseOutput CheckDatabase();
}