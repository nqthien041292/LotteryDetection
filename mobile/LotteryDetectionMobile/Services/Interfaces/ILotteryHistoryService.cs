using LotteryDetectionMobile.Models.Lottery;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface ILotteryHistoryService
{
    Task<IReadOnlyList<LotteryHistoryEntry>> GetEntriesAsync(CancellationToken ct = default);
}
