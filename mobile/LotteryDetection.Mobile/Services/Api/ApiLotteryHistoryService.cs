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
    private const string HistoryPath = "api/services/app/LotteryAnalysis/GetHistory?maxResultCount=100";

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

    public async Task<IReadOnlyList<LotteryHistoryEntry>> GetEntriesAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, HistoryPath);

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

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"[ApiLotteryHistoryService] HTTP {(int)response.StatusCode}");
            return Array.Empty<LotteryHistoryEntry>();
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var paged = ParseEnvelope(body);
        if (paged?.Items == null) return Array.Empty<LotteryHistoryEntry>();

        return paged.Items
            .Select(MapToEntry)
            .OrderByDescending(e => e.CapturedAt)
            .ToList();
    }

    private static PagedDto? ParseEnvelope(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.TryGetProperty("result", out var r) ? r : doc.RootElement;
        return JsonSerializer.Deserialize<PagedDto>(result.GetRawText(), JsonOptions);
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
