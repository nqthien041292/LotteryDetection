using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Api;

/// <summary>
///     Real backend implementation of <see cref="ILotteryDetectionService" />.
///     POSTs the captured image to <c>/api/services/app/LotteryAnalysis/AnalyzeTicket</c>
///     and maps the ABP response envelope to <see cref="LotteryTicketResult" />.
/// </summary>
public class ApiLotteryDetectionService : ILotteryDetectionService
{
    private const string AnalyzePath = "api/services/app/LotteryAnalysis/AnalyzeTicket";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public ApiLotteryDetectionService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<LotteryTicketResult> AnalyzeAsync(string imagePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            throw new FileNotFoundException("Không tìm thấy ảnh đã chụp.", imagePath);

        await using var fileStream = File.OpenRead(imagePath);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GuessContentType(imagePath));

        using var content = new MultipartFormDataContent
        {
            { streamContent, "file", Path.GetFileName(imagePath) }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, AnalyzePath) { Content = content };

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
                Debug.WriteLine($"[ApiLotteryDetectionService] Token unavailable: {ex.Message}");
            }
        }

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(ExtractErrorMessage(body) ?? $"HTTP {(int)response.StatusCode}");

        var dto = ParseAbpEnvelope(body)
                  ?? throw new InvalidOperationException("Backend trả về dữ liệu trống.");

        return new LotteryTicketResult
        {
            Province = dto.Province ?? string.Empty,
            DrawDate = dto.DrawDate ?? DateTime.Today,
            TicketNumber = dto.TicketNumber ?? string.Empty,
            IsWinner = dto.IsWinner ?? false,
            MatchedPrize = dto.MatchedPrize,
            PrizeAmount = dto.PrizeAmount.HasValue ? (long?)dto.PrizeAmount.Value : null,
            Confidence = dto.Confidence.HasValue ? (double)dto.Confidence.Value : 0d,
            Notes = dto.Notes,
            ImagePath = imagePath,
            AnalyzedAt = dto.CreationTime ?? DateTime.Now
        };
    }

    private static TicketAnalysisDto? ParseAbpEnvelope(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var resultEl = root.TryGetProperty("result", out var r) ? r : root;
        return JsonSerializer.Deserialize<TicketAnalysisDto>(resultEl.GetRawText(), JsonOptions);
    }

    private static string? ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch (JsonException)
        {
        }
        return body.Length > 200 ? body[..200] : body;
    }

    private static string GuessContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".heic" => "image/heic",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private sealed class TicketAnalysisDto
    {
        public Guid Id { get; set; }
        public Guid? ImageBinaryObjectId { get; set; }
        public string? Province { get; set; }
        public DateTime? DrawDate { get; set; }
        public string? TicketNumber { get; set; }
        public string? DrawType { get; set; }
        public decimal? Confidence { get; set; }
        public bool? IsWinner { get; set; }
        public string? MatchedPrize { get; set; }
        public decimal? PrizeAmount { get; set; }
        public string? Notes { get; set; }
        public byte Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CreationTime { get; set; }
    }
}
