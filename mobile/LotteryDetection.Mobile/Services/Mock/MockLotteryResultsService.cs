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

        var now = DateTime.Now;
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        // Lấy kết quả từng miền dựa trên thời gian mở thưởng thực tế:
        // Miền Nam (Nam): quay từ 16:15 -> hoàn tất tầm 16:45
        // Miền Trung (Trung): quay từ 17:15 -> hoàn tất tầm 17:45
        // Miền Bắc (Bac): quay từ 18:15 -> hoàn tất tầm 18:45
        var showNamToday = now.Hour > 16 || (now.Hour == 16 && now.Minute >= 45);
        var showTrungToday = now.Hour > 17 || (now.Hour == 17 && now.Minute >= 45);
        var showBacToday = now.Hour > 18 || (now.Hour == 18 && now.Minute >= 45);

        var dateNam = showNamToday ? today : yesterday;
        var dateTrung = showTrungToday ? today : yesterday;
        var dateBac = showBacToday ? today : yesterday;

        return new List<LotteryRegionDraw>
        {
            BuildDraw(LotteryRegion.Bac, "Miền Bắc", "Hà Nội (XSMB)", dateBac),
            
            BuildDraw(LotteryRegion.Trung, "Miền Trung", "Đà Nẵng", dateTrung),
            BuildDraw(LotteryRegion.Trung, "Miền Trung", "Khánh Hòa", dateTrung),
            
            BuildDraw(LotteryRegion.Nam, "Miền Nam", "TP. HCM", dateNam),
            BuildDraw(LotteryRegion.Nam, "Miền Nam", "Đồng Tháp", dateNam),
            BuildDraw(LotteryRegion.Nam, "Miền Nam", "Cà Mau", dateNam)
        };
    }

    private LotteryRegionDraw BuildDraw(LotteryRegion region, string label, string province, DateTime date)
    {
        var draw = new LotteryRegionDraw
        {
            Region = region,
            RegionLabel = label,
            ProvinceLabel = province,
            DrawDate = date
        };

        // For back compatibility & simplicity, we also populate a single Province Header matching the name
        draw.Provinces.Add(new LotteryProvinceHeader { Name = province, ColumnIndex = 0 });

        // Tier setups: label, digits, count, isSpecial
        var tierConfigs = region == LotteryRegion.Bac ? new[]
        {
            new { Label = "Đặc biệt", IsSpecial = true, Digits = 6, Count = 1 },
            new { Label = "Giải nhất", IsSpecial = false, Digits = 5, Count = 1 },
            new { Label = "Giải nhì", IsSpecial = false, Digits = 5, Count = 2 },
            new { Label = "Giải ba", IsSpecial = false, Digits = 5, Count = 6 },
            new { Label = "Giải tư", IsSpecial = false, Digits = 4, Count = 4 },
            new { Label = "Giải năm", IsSpecial = false, Digits = 4, Count = 6 },
            new { Label = "Giải sáu", IsSpecial = false, Digits = 3, Count = 3 },
            new { Label = "Giải bảy", IsSpecial = false, Digits = 2, Count = 4 },
            new { Label = "Giải tám", IsSpecial = false, Digits = 2, Count = 0 } // No Giải 8 for MB
        } : new[]
        {
            new { Label = "Đặc biệt", IsSpecial = true, Digits = 6, Count = 1 },
            new { Label = "Giải nhất", IsSpecial = false, Digits = 5, Count = 1 },
            new { Label = "Giải nhì", IsSpecial = false, Digits = 5, Count = 1 },
            new { Label = "Giải ba", IsSpecial = false, Digits = 5, Count = 2 },
            new { Label = "Giải tư", IsSpecial = false, Digits = 5, Count = 7 },
            new { Label = "Giải năm", IsSpecial = false, Digits = 4, Count = 1 },
            new { Label = "Giải sáu", IsSpecial = false, Digits = 3, Count = 3 },
            new { Label = "Giải bảy", IsSpecial = false, Digits = 3, Count = 1 },
            new { Label = "Giải tám", IsSpecial = false, Digits = 2, Count = 1 }
        };

        foreach (var config in tierConfigs)
        {
            if (config.Count == 0) continue; // Skip if no draws for this tier (like Giải 8 in MB)

            var nums = Enumerable.Range(0, config.Count)
                .Select(_ => RandomDigits(config.Digits));

            var cellString = string.Join(" · ", nums);

            // Add legacy single tier
            draw.Prizes.Add(new LotteryPrizeTier
            {
                TierLabel = config.Label,
                IsSpecial = config.IsSpecial,
                Numbers = cellString
            });

            // Add row structure for any other logic
            var row = new LotteryRowDraw
            {
                TierLabel = config.Label,
                IsSpecial = config.IsSpecial
            };
            row.Numbers.Add(new LotteryNumberCol
            {
                Number = cellString,
                ColumnIndex = 0
            });
            draw.Rows.Add(row);
        }

        return draw;
    }

    private string RandomDigits(int length)
    {
        var max = (int)Math.Pow(10, length);
        return random.Next(0, max).ToString(new string('0', length));
    }
}
