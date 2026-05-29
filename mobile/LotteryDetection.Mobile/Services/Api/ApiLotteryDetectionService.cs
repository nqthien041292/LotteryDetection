using System.Diagnostics;
using System.Net;
using System.Text;
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
    private const string RegisterTokenPath = "api/services/app/DeviceToken/RegisterDeviceToken";

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

    public async Task RegisterDeviceTokenAsync(string token, string deviceType, string deviceName)
    {
        if (!_authService.IsSignedIn) return;

        var input = new
        {
            token = token,
            deviceType = deviceType,
            deviceName = deviceName
        };

        var json = JsonSerializer.Serialize(input, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, RegisterTokenPath) { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var accessToken = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[ApiLotteryDetectionService] Failed to register device token: {error}");
        }
    }

    public async Task<List<LotteryTicketResult>> AnalyzeAsync(string imagePath, CancellationToken ct)
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiLotteryDetectionService] Token unavailable: {ex.Message}");
            }
        }

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (IsAuthRedirect(response))
            throw new UnauthorizedAccessException("Bạn cần đăng nhập lại trước khi AI dò xổ số.");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(ExtractErrorMessage(body) ?? $"HTTP {(int)response.StatusCode}");

        if (!IsJsonResponse(response, body))
            throw new InvalidOperationException(ExtractNonJsonMessage(response, body));

        var dtos = ParseAbpEnvelope(body);
        if (dtos == null || dtos.Count == 0)
            throw new InvalidOperationException("Backend trả về dữ liệu trống.");

        return dtos.Select(dto => new LotteryTicketResult
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
        }).ToList();
    }

    private static List<TicketAnalysisDto>? ParseAbpEnvelope(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var resultEl = root.TryGetProperty("result", out var r) ? r : root;
        return JsonSerializer.Deserialize<List<TicketAnalysisDto>>(resultEl.GetRawText(), JsonOptions);
    }

    private static bool IsAuthRedirect(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return true;

        var statusCode = (int)response.StatusCode;
        if (statusCode is < 300 or > 399) return false;

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        return location.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
               location.Contains("ReturnUrl=", StringComparison.OrdinalIgnoreCase) ||
               location.Contains("statusCode=401", StringComparison.OrdinalIgnoreCase) ||
               location.Contains("statusCode=403", StringComparison.OrdinalIgnoreCase);
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

    private static string ExtractNonJsonMessage(HttpResponseMessage response, string body)
    {
        if (body.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return "Backend trả về trang đăng nhập thay vì JSON. Vui lòng đăng nhập lại rồi thử phân tích.";

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "không rõ";
        return $"Backend không trả về JSON hợp lệ (Content-Type: {contentType}).";
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
            if (body.Contains("<html", StringComparison.OrdinalIgnoreCase))
                return "Lỗi hệ thống (Server Error). Vui lòng thử lại sau.";
        }
        
        var plain = body.Trim();
        return plain.Length > 200 ? plain[..200] : plain;
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
