using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockLotteryHistoryService : ILotteryHistoryService
{
    // Static arrays declared FIRST — BuildSeed() reads them during the ctor, so
    // they must already be initialized before `Instance = new()` runs.
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

    public Task<IReadOnlyList<LotteryHistoryEntry>> GetEntriesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<LotteryHistoryEntry> snapshot = entries
            .OrderByDescending(e => e.CapturedAt)
            .ToList();
        return Task.FromResult(snapshot);
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

        // Eight entries spread across the last week — two of them winners.
        var schedule = new (int hoursAgo, bool isWinner)[]
        {
            (1, false),
            (5, true),
            (24, false),
            (29, false),
            (48, true),
            (74, false),
            (96, false),
            (120, false)
        };

        foreach (var (hoursAgo, isWinner) in schedule)
        {
            var capturedAt = now.AddHours(-hoursAgo);
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
