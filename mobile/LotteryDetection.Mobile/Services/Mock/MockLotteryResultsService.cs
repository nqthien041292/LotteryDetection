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

        var provincesNam = dateNam.DayOfWeek switch
        {
            DayOfWeek.Monday => new[] { "TP. HCM", "Đồng Tháp", "Cà Mau" },
            DayOfWeek.Tuesday => new[] { "Bến Tre", "Vũng Tàu", "Bạc Liêu" },
            DayOfWeek.Wednesday => new[] { "Đồng Nai", "Cần Thơ", "Sóc Trăng" },
            DayOfWeek.Thursday => new[] { "Tây Ninh", "An Giang", "Bình Thuận" },
            DayOfWeek.Friday => new[] { "Vĩnh Long", "Bình Dương", "Trà Vinh" },
            DayOfWeek.Saturday => new[] { "TP. HCM", "Long An", "Bình Phước", "Hậu Giang" },
            DayOfWeek.Sunday => new[] { "Tiền Giang", "Kiên Giang", "Đà Lạt" },
            _ => new[] { "Tiền Giang", "Kiên Giang", "Đà Lạt" }
        };

        var provincesTrung = dateTrung.DayOfWeek switch
        {
            DayOfWeek.Monday => new[] { "Thừa Thiên Huế", "Phú Yên" },
            DayOfWeek.Tuesday => new[] { "Đắk Lắk", "Quảng Nam" },
            DayOfWeek.Wednesday => new[] { "Đà Nẵng", "Khánh Hòa" },
            DayOfWeek.Thursday => new[] { "Bình Định", "Quảng Trị", "Quảng Bình" },
            DayOfWeek.Friday => new[] { "Gia Lai", "Ninh Thuận" },
            DayOfWeek.Saturday => new[] { "Đà Nẵng", "Quảng Ngãi", "Đắk Nông" },
            DayOfWeek.Sunday => new[] { "Khánh Hòa", "Kon Tum" },
            _ => new[] { "Khánh Hòa", "Kon Tum" }
        };

        var list = new List<LotteryRegionDraw>();
        list.Add(BuildDraw(LotteryRegion.Bac, "Miền Bắc", "Hà Nội (XSMB)", dateBac));

        foreach (var p in provincesTrung)
        {
            list.Add(BuildDraw(LotteryRegion.Trung, "Miền Trung", p, dateTrung));
        }

        foreach (var p in provincesNam)
        {
            list.Add(BuildDraw(LotteryRegion.Nam, "Miền Nam", p, dateNam));
        }

        return list;
    }

    public async Task<IReadOnlyList<LotteryRegionDraw>> GetLiveResultsAsync(CancellationToken ct = default)
    {
        // Light artificial latency
        await Task.Delay(200, ct);

        // Fetch standard results for today
        var standardResults = await GetTodayResultsAsync(ct);

        var now = DateTime.Now;
        var today = DateTime.Today;
        var time = now.TimeOfDay;

        foreach (var draw in standardResults)
        {
            // Determine if this region is in its live drawing window
            bool isLive = false;
            double progress = 1.0; // 100% completed by default

            if (draw.Region == LotteryRegion.Nam)
            {
                isLive = time >= new TimeSpan(16, 10, 0) && time <= new TimeSpan(16, 45, 0);
                if (isLive)
                {
                    var elapsed = (time - new TimeSpan(16, 10, 0)).TotalMinutes;
                    progress = Math.Clamp(elapsed / 30.0, 0.0, 1.0);
                }
            }
            else if (draw.Region == LotteryRegion.Trung)
            {
                isLive = time >= new TimeSpan(17, 10, 0) && time <= new TimeSpan(17, 45, 0);
                if (isLive)
                {
                    var elapsed = (time - new TimeSpan(17, 10, 0)).TotalMinutes;
                    progress = Math.Clamp(elapsed / 30.0, 0.0, 1.0);
                }
            }
            else if (draw.Region == LotteryRegion.Bac)
            {
                isLive = time >= new TimeSpan(18, 10, 0) && time <= new TimeSpan(18, 45, 0);
                if (isLive)
                {
                    var elapsed = (time - new TimeSpan(18, 10, 0)).TotalMinutes;
                    progress = Math.Clamp(elapsed / 30.0, 0.0, 1.0);
                }
            }

            // If the drawing date is in the past (yesterday) or today's drawing window has already finished,
            // we show 100% complete results. Otherwise (e.g. earlier today before the draw starts),
            // we can simulate a 60% complete drawing for demo/preview purposes.
            if (draw.DrawDate < today)
            {
                progress = 1.0;
            }
            else // draw.DrawDate == today
            {
                if (draw.Region == LotteryRegion.Nam && time > new TimeSpan(16, 45, 0))
                {
                    progress = 1.0;
                }
                else if (draw.Region == LotteryRegion.Trung && time > new TimeSpan(17, 45, 0))
                {
                    progress = 1.0;
                }
                else if (draw.Region == LotteryRegion.Bac && time > new TimeSpan(18, 45, 0))
                {
                    progress = 1.0;
                }
                else if (!isLive)
                {
                    progress = 0.6; // Demo preview state
                }
            }

            int totalPrizes = draw.Prizes.Count;
            // Southern/Central: drawn 8 to Special. Northern: 7 to Special.
            // Prizes are added as: Special (0), 1, 2, 3, 4, 5, 6, 7, 8 (last).
            // So higher indices are drawn first.
            int drawnCount = (int)Math.Round(progress * totalPrizes);
            drawnCount = Math.Clamp(drawnCount, 1, totalPrizes);

            for (int i = 0; i < totalPrizes; i++)
            {
                var prize = draw.Prizes[i];
                int drawOrderIndex = totalPrizes - 1 - i; 
                prize.IsDrawn = drawOrderIndex < drawnCount;
            }
        }

        return standardResults;
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
