using LotteryDetection.Mobile.Models.Lottery;

namespace LotteryDetection.Mobile.Services.Interfaces;

public class LotteryHistoryPageResult
{
    public int TotalCount { get; set; }
    public IReadOnlyList<LotteryHistoryEntry> Items { get; set; } = Array.Empty<LotteryHistoryEntry>();
}

public class LotteryHistoryStats
{
    public int TotalCount { get; set; }
    public int WinCount { get; set; }
    public long TotalWinnings { get; set; }
    public long BiggestWin { get; set; }
}

public interface ILotteryHistoryService
{
    Task<LotteryHistoryPageResult> GetEntriesAsync(int skipCount, int maxResultCount, CancellationToken ct = default);
    Task<LotteryHistoryStats?> GetStatsAsync(CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(string id, CancellationToken ct = default);
}
