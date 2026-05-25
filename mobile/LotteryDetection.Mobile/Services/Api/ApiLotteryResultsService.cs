using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Api;

public class ApiLotteryResultsService : ILotteryResultsService
{
    private const string GetResultsPath = "api/services/app/LotteryAnalysis/GetDrawResults";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public ApiLotteryResultsService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public bool IsLiveDrawingTime()
    {
        var now = DateTime.Now;
        var time = now.TimeOfDay;

        bool isNamLive = time >= new TimeSpan(16, 10, 0) && time <= new TimeSpan(16, 45, 0);
        bool isTrungLive = time >= new TimeSpan(17, 10, 0) && time <= new TimeSpan(17, 45, 0);
        bool isBacLive = time >= new TimeSpan(18, 10, 0) && time <= new TimeSpan(18, 45, 0);

        return isNamLive || isTrungLive || isBacLive;
    }

    public async Task<IReadOnlyList<LotteryRegionDraw>> GetTodayResultsAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        // Determine query date based on drawing schedules
        var showNamToday = now.Hour > 16 || (now.Hour == 16 && now.Minute >= 10);
        var showTrungToday = now.Hour > 17 || (now.Hour == 17 && now.Minute >= 10);
        var showBacToday = now.Hour > 18 || (now.Hour == 18 && now.Minute >= 10);

        var dateNam = showNamToday ? today : yesterday;
        var dateTrung = showTrungToday ? today : yesterday;
        var dateBac = showBacToday ? today : yesterday;

        try
        {
            // We will fetch real results from the backend. Since the backend returns results by a single date,
            // we will query for both today and yesterday, then filter the provinces/regions accordingly.
            var todayResults = await FetchDrawResultsFromApiAsync(today, ct);
            var yesterdayResults = await FetchDrawResultsFromApiAsync(yesterday, ct);

            var allRealResults = new List<LotteryDrawResultDto>();
            if (todayResults != null) allRealResults.AddRange(todayResults);
            if (yesterdayResults != null) allRealResults.AddRange(yesterdayResults);

            var list = new List<LotteryRegionDraw>();

            // Process Miền Bắc
            var bacResults = allRealResults.FirstOrDefault(r => IsRegion(r.Province, LotteryRegion.Bac) && r.DrawDate.Date == dateBac.Date);
            if (bacResults != null)
            {
                list.Add(MapToRegionDraw(bacResults, LotteryRegion.Bac, "Miền Bắc"));
            }
            else
            {
                list.Add(BuildMockFallback(LotteryRegion.Bac, "Miền Bắc", "Hà Nội (XSMB)", dateBac));
            }

            // Process Miền Trung
            var activeTrungProvinces = GetActiveProvincesForDay(dateTrung, LotteryRegion.Trung);
            foreach (var province in activeTrungProvinces)
            {
                var realResult = allRealResults.FirstOrDefault(r => MatchProvince(r.Province, province) && r.DrawDate.Date == dateTrung.Date);
                if (realResult != null)
                {
                    list.Add(MapToRegionDraw(realResult, LotteryRegion.Trung, "Miền Trung"));
                }
                else
                {
                    list.Add(BuildMockFallback(LotteryRegion.Trung, "Miền Trung", province, dateTrung));
                }
            }

            // Process Miền Nam
            var activeNamProvinces = GetActiveProvincesForDay(dateNam, LotteryRegion.Nam);
            foreach (var province in activeNamProvinces)
            {
                var realResult = allRealResults.FirstOrDefault(r => MatchProvince(r.Province, province) && r.DrawDate.Date == dateNam.Date);
                if (realResult != null)
                {
                    list.Add(MapToRegionDraw(realResult, LotteryRegion.Nam, "Miền Nam"));
                }
                else
                {
                    list.Add(BuildMockFallback(LotteryRegion.Nam, "Miền Nam", province, dateNam));
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiLotteryResultsService] GetTodayResultsAsync failed: {ex.Message}. Returning empty fallbacks.");
            
            var list = new List<LotteryRegionDraw>();
            list.Add(BuildMockFallback(LotteryRegion.Bac, "Miền Bắc", "Hà Nội (XSMB)", dateBac));

            var activeTrungProvinces = GetActiveProvincesForDay(dateTrung, LotteryRegion.Trung);
            foreach (var province in activeTrungProvinces)
            {
                list.Add(BuildMockFallback(LotteryRegion.Trung, "Miền Trung", province, dateTrung));
            }

            var activeNamProvinces = GetActiveProvincesForDay(dateNam, LotteryRegion.Nam);
            foreach (var province in activeNamProvinces)
            {
                list.Add(BuildMockFallback(LotteryRegion.Nam, "Miền Nam", province, dateNam));
            }

            return list;
        }
    }

    public async Task<IReadOnlyList<LotteryRegionDraw>> GetLiveResultsAsync(CancellationToken ct = default)
    {
        // For Live Results, we fetch real results from today/yesterday as appropriate.
        return await GetTodayResultsAsync(ct);
    }

    private async Task<List<LotteryDrawResultDto>?> FetchDrawResultsFromApiAsync(DateTime date, CancellationToken ct)
    {
        var url = $"{GetResultsPath}?drawDate={date:yyyy-MM-dd}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        if (_authService.IsSignedIn)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            catch
            {
                // Ignore token errors
            }
        }

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var resultEl = root.TryGetProperty("result", out var r) ? r : root;
        return JsonSerializer.Deserialize<List<LotteryDrawResultDto>>(resultEl.GetRawText(), JsonOptions);
    }

    private static bool IsRegion(string province, LotteryRegion region)
    {
        var p = province.ToLowerInvariant();
        if (region == LotteryRegion.Bac)
            return p.Contains("bắc") || p.Contains("bac") || p.Contains("hà nội") || p.Contains("xsmb");
        
        if (region == LotteryRegion.Trung)
        {
            return p.Contains("huế") || p.Contains("hue") || p.Contains("phú yên") || p.Contains("phu_yen") ||
                   p.Contains("đắk lắk") || p.Contains("dak lak") || p.Contains("quảng nam") || p.Contains("quang nam") ||
                   p.Contains("đà nẵng") || p.Contains("da nang") || p.Contains("khánh hòa") || p.Contains("khanh hoa") ||
                   p.Contains("bình định") || p.Contains("binh dinh") || p.Contains("quảng trị") || p.Contains("quang tri") ||
                   p.Contains("quảng bình") || p.Contains("quang binh") || p.Contains("gia lai") ||
                   p.Contains("ninh thuận") || p.Contains("ninh thuan") || p.Contains("đắk nông") || p.Contains("dak nong") ||
                   p.Contains("quảng ngãi") || p.Contains("quang ngai") || p.Contains("kon tum");
        }

        if (region == LotteryRegion.Nam)
        {
            return p.Contains("hcm") || p.Contains("hồ chí minh") || p.Contains("ho chi minh") || p.Contains("sài gòn") || p.Contains("sai_gon") ||
                   p.Contains("đồng tháp") || p.Contains("dong thap") || p.Contains("cà mau") || p.Contains("ca mau") ||
                   p.Contains("bến tre") || p.Contains("ben tre") || p.Contains("vũng tàu") || p.Contains("vung tau") ||
                   p.Contains("bạc liêu") || p.Contains("bac lieu") || p.Contains("đồng nai") || p.Contains("dong nai") ||
                   p.Contains("cần thơ") || p.Contains("can tho") || p.Contains("sóc trăng") || p.Contains("soc trang") ||
                   p.Contains("tây ninh") || p.Contains("tay ninh") || p.Contains("an giang") || p.Contains("bình thuận") || p.Contains("binh thuan") ||
                   p.Contains("vĩnh long") || p.Contains("vinh long") || p.Contains("bình dương") || p.Contains("binh duong") ||
                   p.Contains("trà vinh") || p.Contains("tra vinh") || p.Contains("long an") || p.Contains("bình phước") || p.Contains("binh phuoc") ||
                   p.Contains("hậu giang") || p.Contains("hau giang") || p.Contains("tiền giang") || p.Contains("tien giang") ||
                   p.Contains("kiên giang") || p.Contains("kien giang") || p.Contains("đà lạt") || p.Contains("da lat") || p.Contains("lâm đồng") || p.Contains("lam dong");
        }

        return false;
    }

    private static bool MatchProvince(string dbProvince, string queryProvince)
    {
        var db = dbProvince.ToLowerInvariant();
        var q = queryProvince.ToLowerInvariant();
        return db == q || db.Contains(q) || q.Contains(db);
    }

    private static LotteryRegionDraw MapToRegionDraw(LotteryDrawResultDto dto, LotteryRegion region, string regionLabel)
    {
        var draw = new LotteryRegionDraw
        {
            Region = region,
            RegionLabel = regionLabel,
            ProvinceLabel = dto.Province,
            DrawDate = dto.DrawDate
        };

        draw.Provinces.Add(new LotteryProvinceHeader { Name = dto.Province, ColumnIndex = 0 });

        var tierConfigs = region == LotteryRegion.Bac ? new[]
        {
            new { Label = "Đặc biệt", IsSpecial = true },
            new { Label = "Giải nhất", IsSpecial = false },
            new { Label = "Giải nhì", IsSpecial = false },
            new { Label = "Giải ba", IsSpecial = false },
            new { Label = "Giải tư", IsSpecial = false },
            new { Label = "Giải năm", IsSpecial = false },
            new { Label = "Giải sáu", IsSpecial = false },
            new { Label = "Giải bảy", IsSpecial = false }
        } : new[]
        {
            new { Label = "Đặc biệt", IsSpecial = true },
            new { Label = "Giải nhất", IsSpecial = false },
            new { Label = "Giải nhì", IsSpecial = false },
            new { Label = "Giải ba", IsSpecial = false },
            new { Label = "Giải tư", IsSpecial = false },
            new { Label = "Giải năm", IsSpecial = false },
            new { Label = "Giải sáu", IsSpecial = false },
            new { Label = "Giải bảy", IsSpecial = false },
            new { Label = "Giải tám", IsSpecial = false }
        };

        var now = DateTime.Now;
        var time = now.TimeOfDay;
        bool isLiveNow = false;

        // Determine if currently in the live drawing time for this region
        if (dto.DrawDate.Date == DateTime.Today)
        {
            if (region == LotteryRegion.Nam)
                isLiveNow = time >= new TimeSpan(16, 10, 0) && time <= new TimeSpan(16, 45, 0);
            else if (region == LotteryRegion.Trung)
                isLiveNow = time >= new TimeSpan(17, 10, 0) && time <= new TimeSpan(17, 45, 0);
            else if (region == LotteryRegion.Bac)
                isLiveNow = time >= new TimeSpan(18, 10, 0) && time <= new TimeSpan(18, 45, 0);
        }

        foreach (var config in tierConfigs)
        {
            string cellString;
            bool isDrawn;

            if (dto.Prizes != null && dto.Prizes.TryGetValue(config.Label, out var nums) && nums != null && nums.Count > 0)
            {
                cellString = string.Join(" · ", nums);
                isDrawn = true;
            }
            else
            {
                cellString = isLiveNow ? "" : "-";
                isDrawn = !isLiveNow;
            }

            draw.Prizes.Add(new LotteryPrizeTier
            {
                TierLabel = config.Label,
                IsSpecial = config.IsSpecial,
                Numbers = cellString,
                IsDrawn = isDrawn
            });

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

    private static LotteryRegionDraw BuildMockFallback(LotteryRegion region, string label, string province, DateTime date)
    {
        var draw = new LotteryRegionDraw
        {
            Region = region,
            RegionLabel = label,
            ProvinceLabel = province,
            DrawDate = date
        };

        draw.Provinces.Add(new LotteryProvinceHeader { Name = province, ColumnIndex = 0 });

        var tierConfigs = region == LotteryRegion.Bac ? new[]
        {
            new { Label = "Đặc biệt", IsSpecial = true, Count = 1 },
            new { Label = "Giải nhất", IsSpecial = false, Count = 1 },
            new { Label = "Giải nhì", IsSpecial = false, Count = 2 },
            new { Label = "Giải ba", IsSpecial = false, Count = 6 },
            new { Label = "Giải tư", IsSpecial = false, Count = 4 },
            new { Label = "Giải năm", IsSpecial = false, Count = 6 },
            new { Label = "Giải sáu", IsSpecial = false, Count = 3 },
            new { Label = "Giải bảy", IsSpecial = false, Count = 4 },
            new { Label = "Giải tám", IsSpecial = false, Count = 0 }
        } : new[]
        {
            new { Label = "Đặc biệt", IsSpecial = true, Count = 1 },
            new { Label = "Giải nhất", IsSpecial = false, Count = 1 },
            new { Label = "Giải nhì", IsSpecial = false, Count = 1 },
            new { Label = "Giải ba", IsSpecial = false, Count = 2 },
            new { Label = "Giải tư", IsSpecial = false, Count = 7 },
            new { Label = "Giải năm", IsSpecial = false, Count = 1 },
            new { Label = "Giải sáu", IsSpecial = false, Count = 3 },
            new { Label = "Giải bảy", IsSpecial = false, Count = 1 },
            new { Label = "Giải tám", IsSpecial = false, Count = 1 }
        };

        var now = DateTime.Now;
        var time = now.TimeOfDay;
        bool isLiveNow = false;

        if (date.Date == DateTime.Today)
        {
            if (region == LotteryRegion.Nam)
                isLiveNow = time >= new TimeSpan(16, 10, 0) && time <= new TimeSpan(16, 45, 0);
            else if (region == LotteryRegion.Trung)
                isLiveNow = time >= new TimeSpan(17, 10, 0) && time <= new TimeSpan(17, 45, 0);
            else if (region == LotteryRegion.Bac)
                isLiveNow = time >= new TimeSpan(18, 10, 0) && time <= new TimeSpan(18, 45, 0);
        }

        foreach (var config in tierConfigs)
        {
            if (config.Count == 0) continue;

            var cellString = isLiveNow ? "" : "-";

            draw.Prizes.Add(new LotteryPrizeTier
            {
                TierLabel = config.Label,
                IsSpecial = config.IsSpecial,
                Numbers = cellString,
                IsDrawn = !isLiveNow
            });

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

    private static string[] GetActiveProvincesForDay(DateTime date, LotteryRegion region)
    {
        if (region == LotteryRegion.Bac) return new[] { "Hà Nội (XSMB)" };

        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => region == LotteryRegion.Trung ? new[] { "Thừa Thiên Huế", "Phú Yên" } : new[] { "TP. HCM", "Đồng Tháp", "Cà Mau" },
            DayOfWeek.Tuesday => region == LotteryRegion.Trung ? new[] { "Đắk Lắk", "Quảng Nam" } : new[] { "Bến Tre", "Vũng Tàu", "Bạc Liêu" },
            DayOfWeek.Wednesday => region == LotteryRegion.Trung ? new[] { "Đà Nẵng", "Khánh Hòa" } : new[] { "Đồng Nai", "Cần Thơ", "Sóc Trăng" },
            DayOfWeek.Thursday => region == LotteryRegion.Trung ? new[] { "Bình Định", "Quảng Trị", "Quảng Bình" } : new[] { "Tây Ninh", "An Giang", "Bình Thuận" },
            DayOfWeek.Friday => region == LotteryRegion.Trung ? new[] { "Gia Lai", "Ninh Thuận" } : new[] { "Vĩnh Long", "Bình Dương", "Trà Vinh" },
            DayOfWeek.Saturday => region == LotteryRegion.Trung ? new[] { "Đà Nẵng", "Quảng Ngãi", "Đắk Nông" } : new[] { "TP. HCM", "Long An", "Bình Phước", "Hậu Giang" },
            DayOfWeek.Sunday => region == LotteryRegion.Trung ? new[] { "Khánh Hòa", "Kon Tum" } : new[] { "Tiền Giang", "Kiên Giang", "Đà Lạt" },
            _ => Array.Empty<string>()
        };
    }

    private sealed class LotteryDrawResultDto
    {
        public string Province { get; set; } = string.Empty;
        public DateTime DrawDate { get; set; }
        public Dictionary<string, List<string>>? Prizes { get; set; }
    }
}
