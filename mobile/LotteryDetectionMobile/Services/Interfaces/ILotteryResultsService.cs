using LotteryDetectionMobile.Models.Lottery;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface ILotteryResultsService
{
    Task<IReadOnlyList<LotteryRegionDraw>> GetTodayResultsAsync(CancellationToken ct = default);
}
