using System.Collections.ObjectModel;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockLotteryResultsService : ILotteryResultsService
{
    public static readonly MockLotteryResultsService Instance = new();

    private readonly Random random = new();

    public async Task<IReadOnlyList<LotteryRegionDraw>> GetTodayResultsAsync(CancellationToken ct = default)
    {
        // Light artificial latency so the loading state is observable.
        await Task.Delay(280, ct);

        var today = DateTime.Today;
        return new List<LotteryRegionDraw>
        {
            BuildDraw(LotteryRegion.Bac, "Miền Bắc", "XSMB", today),
            BuildDraw(LotteryRegion.Trung, "Miền Trung", "Đà Nẵng · Khánh Hòa", today),
            BuildDraw(LotteryRegion.Nam, "Miền Nam", "TP. HCM · Đồng Tháp · Cà Mau", today)
        };
    }

    private LotteryRegionDraw BuildDraw(LotteryRegion region, string label, string provinces, DateTime date)
    {
        return new LotteryRegionDraw
        {
            Region = region,
            RegionLabel = label,
            ProvinceLabel = provinces,
            DrawDate = date,
            Prizes = new ObservableCollection<LotteryPrizeTier>
            {
                new() { TierLabel = "Đặc biệt", Numbers = RandomDigits(6), IsSpecial = true },
                new() { TierLabel = "Giải nhất", Numbers = RandomDigits(5) },
                new() { TierLabel = "Giải nhì", Numbers = RandomDigits(5) + " · " + RandomDigits(5) },
                new() { TierLabel = "Giải ba", Numbers = RandomDigits(5) + " · " + RandomDigits(5) + " · " + RandomDigits(5) },
                new() { TierLabel = "Giải tư", Numbers = RandomDigits(4) + " · " + RandomDigits(4) + " · " + RandomDigits(4) },
                new() { TierLabel = "Giải năm", Numbers = RandomDigits(4) + " · " + RandomDigits(4) },
                new() { TierLabel = "Giải sáu", Numbers = RandomDigits(3) + " · " + RandomDigits(3) + " · " + RandomDigits(3) },
                new() { TierLabel = "Giải bảy", Numbers = RandomDigits(2) + " · " + RandomDigits(2) }
            }
        };
    }

    private string RandomDigits(int length)
    {
        var max = (int)Math.Pow(10, length);
        return random.Next(0, max).ToString(new string('0', length));
    }
}
