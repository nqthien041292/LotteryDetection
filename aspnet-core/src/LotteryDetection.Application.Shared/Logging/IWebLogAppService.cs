using Abp.Application.Services;
using LotteryDetection.Dto;
using LotteryDetection.Logging.Dto;

namespace LotteryDetection.Logging;

public interface IWebLogAppService : IApplicationService
{
    GetLatestWebLogsOutput GetLatestWebLogs();

    FileDto DownloadWebLogs();
}

