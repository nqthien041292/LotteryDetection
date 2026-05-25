using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockLotteryHistoryService : ILotteryHistoryService
{
    private static readonly string[] Provinces =
    {
        "TP. Hồ Chí Minh", "Đồng Nai", "Cần Thơ", "Bình Dương",
        "Vũng Tàu", "Tiền Giang", "Kiên Giang", "Long An", "Đà Nẵng"
    };

    private static readonly (string Tier, long Amount)[] Prizes =
    {
        ("Giải khuyến khích", 50_000),
        ("Giải tám", 100_000),
        ("Giải bảy", 200_000),
        ("Giải sáu", 400_000),
        ("Giải năm", 1_000_000),
        ("Giải tư", 3_000_000)
    };

    public static readonly MockLotteryHistoryService Instance = new();

    private readonly List<LotteryHistoryEntry> entries;

    public MockLotteryHistoryService()
    {
        entries = BuildSeed();
    }

    public Task<LotteryHistoryPageResult> GetEntriesAsync(int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        var sorted = entries.OrderByDescending(e => e.CapturedAt).ToList();
        var paged = sorted.Skip(skipCount).Take(maxResultCount).ToList();

        var result = new LotteryHistoryPageResult
        {
            TotalCount = sorted.Count,
            Items = paged
        };
        return Task.FromResult(result);
    }

    public Task<LotteryHistoryStats?> GetStatsAsync(CancellationToken ct = default)
    {
        var winners = entries.Where(e => e.IsWinner).ToList();
        var stats = new LotteryHistoryStats
        {
            TotalCount = entries.Count,
            WinCount = winners.Count,
            TotalWinnings = winners.Sum(w => w.PrizeAmount ?? 0),
            BiggestWin = winners.Count > 0 ? winners.Max(w => w.PrizeAmount ?? 0L) : 0
        };
        return Task.FromResult<LotteryHistoryStats?>(stats);
    }

    public Task<bool> DeleteEntryAsync(string id, CancellationToken ct = default)
    {
        var item = entries.FirstOrDefault(e => e.Id == id);
        if (item != null)
        {
            entries.Remove(item);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private static List<LotteryHistoryEntry> BuildSeed()
    {
        var rng = new Random(20260519);
        var result = new List<LotteryHistoryEntry>();
        var now = DateTime.Now;

        // Twenty-five entries to make paging clearly testable - some winners.
        for (int i = 0; i < 25; i++)
        {
            bool isWinner = (i == 3 || i == 8 || i == 15);
            var capturedAt = now.AddHours(-i * 4 - 1);
            var province = Provinces[rng.Next(Provinces.Length)];
            var ticket = rng.Next(0, 1_000_000).ToString("D6");
            var entry = new LotteryHistoryEntry
            {
                CapturedAt = capturedAt,
                Province = province,
                TicketNumber = ticket,
                DrawDate = capturedAt.Date,
                IsWinner = isWinner
            };
            if (isWinner)
            {
                var prize = Prizes[rng.Next(Prizes.Length)];
                entry.MatchedPrize = prize.Tier;
                entry.PrizeAmount = prize.Amount;
            }
            result.Add(entry);
        }

        return result;
    }
}
