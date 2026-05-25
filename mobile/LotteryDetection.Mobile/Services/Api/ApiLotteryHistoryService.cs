using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Api;

public class ApiLotteryHistoryService : ILotteryHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public ApiLotteryHistoryService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    private async Task AppendAuthHeaderAsync(HttpRequestMessage request)
    {
        if (_authService.IsSignedIn)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiLotteryHistoryService] Token unavailable: {ex.Message}");
            }
        }
    }

    public async Task<LotteryHistoryPageResult> GetEntriesAsync(int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        string path = $"api/services/app/LotteryAnalysis/GetHistory?skipCount={skipCount}&maxResultCount={maxResultCount}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        await AppendAuthHeaderAsync(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"[ApiLotteryHistoryService] HTTP {(int)response.StatusCode}");
            return new LotteryHistoryPageResult();
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!IsJsonResponse(response, body))
        {
            Debug.WriteLine("[ApiLotteryHistoryService] Backend returned non-JSON history response.");
            return new LotteryHistoryPageResult();
        }

        var paged = ParseEnvelope<PagedDto>(body);
        if (paged?.Items == null) return new LotteryHistoryPageResult();

        var mapped = paged.Items
            .Select(MapToEntry)
            .OrderByDescending(e => e.CapturedAt)
            .ToList();

        return new LotteryHistoryPageResult
        {
            TotalCount = paged.TotalCount,
            Items = mapped
        };
    }

    public async Task<LotteryHistoryStats?> GetStatsAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/services/app/LotteryAnalysis/GetHistoryStats");
        await AppendAuthHeaderAsync(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"[ApiLotteryHistoryService] Stats HTTP {(int)response.StatusCode}");
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!IsJsonResponse(response, body))
        {
            Debug.WriteLine("[ApiLotteryHistoryService] Backend returned non-JSON stats response.");
            return null;
        }

        return ParseEnvelope<LotteryHistoryStats>(body);
    }

    public async Task<bool> DeleteEntryAsync(string id, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guidId)) return false;

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/services/app/LotteryAnalysis/Delete");
        await AppendAuthHeaderAsync(request);

        var payload = new { id = guidId };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"[ApiLotteryHistoryService] Delete HTTP {(int)response.StatusCode}");
            return false;
        }

        return true;
    }

    private static T? ParseEnvelope<T>(string body) where T : class
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.TryGetProperty("result", out var r) ? r : doc.RootElement;
        return JsonSerializer.Deserialize<T>(result.GetRawText(), JsonOptions);
    }

    private static bool IsJsonResponse(HttpResponseMessage response, string body)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var trimmed = body.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) ||
               trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static LotteryHistoryEntry MapToEntry(TicketAnalysisDto dto) => new()
    {
        Id = dto.Id.ToString("N"),
        CapturedAt = dto.CreationTime ?? DateTime.Now,
        Province = dto.Province ?? string.Empty,
        TicketNumber = dto.TicketNumber ?? string.Empty,
        DrawDate = dto.DrawDate ?? (dto.CreationTime?.Date ?? DateTime.Today),
        IsWinner = dto.IsWinner ?? false,
        MatchedPrize = dto.MatchedPrize,
        PrizeAmount = dto.PrizeAmount.HasValue ? (long?)dto.PrizeAmount.Value : null
    };

    private sealed class PagedDto
    {
        public int TotalCount { get; set; }
        public List<TicketAnalysisDto>? Items { get; set; }
    }

    private sealed class TicketAnalysisDto
    {
        public Guid Id { get; set; }
        public string? Province { get; set; }
        public DateTime? DrawDate { get; set; }
        public string? TicketNumber { get; set; }
        public bool? IsWinner { get; set; }
        public string? MatchedPrize { get; set; }
        public decimal? PrizeAmount { get; set; }
        public DateTime? CreationTime { get; set; }
    }
}
