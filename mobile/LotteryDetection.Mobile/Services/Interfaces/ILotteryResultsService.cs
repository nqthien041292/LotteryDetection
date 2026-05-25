using LotteryDetection.Mobile.Models.Lottery;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface ILotteryResultsService
{
    Task<IReadOnlyList<LotteryRegionDraw>> GetTodayResultsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LotteryRegionDraw>> GetLiveResultsAsync(CancellationToken ct = default);
    bool IsLiveDrawingTime();
}
