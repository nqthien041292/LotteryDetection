using LotteryDetection.Mobile.Models.Lottery;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface ILotteryHistoryService
{
    Task<IReadOnlyList<LotteryHistoryEntry>> GetEntriesAsync(CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(string id, CancellationToken ct = default);
}
