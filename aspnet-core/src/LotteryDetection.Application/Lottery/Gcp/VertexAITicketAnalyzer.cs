using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.UI;
using Castle.Core.Logging;
using Google.Apis.Auth.OAuth2;

namespace LotteryDetection.Lottery.Gcp;

public class VertexAITicketAnalyzer : IVertexAITicketAnalyzer, ITransientDependency
{
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    private readonly GoogleCloudConfiguration _config;

    public VertexAITicketAnalyzer(GoogleCloudConfiguration config)
    {
        _config = config;
    }

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public async Task<System.Collections.Generic.List<VertexAIAnalysisResult>> AnalyzeAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_config.IsConfigured)
        {
            throw new UserFriendlyException(
                "Google Cloud chưa được cấu hình. Vui lòng set 'GoogleCloud:ProjectId' trong appsettings.");
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var endpoint = BuildEndpoint();
        var requestBody = BuildRequestBody(imageBytes, contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error("Vertex AI request failed", ex);
            throw new UserFriendlyException("Không gọi được Vertex AI. Vui lòng thử lại.", ex.Message);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Logger.Error($"Vertex AI returned {(int)response.StatusCode}: {responseBody}");
            throw new UserFriendlyException(
                $"Vertex AI lỗi {(int)response.StatusCode}",
                Truncate(responseBody, 500));
        }

        return ParseResponse(responseBody);
    }

    private string BuildEndpoint()
    {
        return $"https://{_config.Location}-aiplatform.googleapis.com" +
               $"/v1/projects/{_config.ProjectId}/locations/{_config.Location}" +
               $"/publishers/google/models/{_config.VertexAIModel}:generateContent";
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        try
        {
            var credential = await GoogleCredential.GetApplicationDefaultAsync(ct);
            if (credential.IsCreateScopedRequired) credential = credential.CreateScoped(CloudPlatformScope);
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to acquire GCP access token via ADC", ex);
            throw new UserFriendlyException(
                "Không lấy được Google credential. Hãy chạy 'gcloud auth application-default login' hoặc cấu hình Workload Identity.",
                ex.Message);
        }
    }

    private string BuildRequestBody(byte[] imageBytes, string contentType)
    {
        var imageBase64 = Convert.ToBase64String(imageBytes);
        var schema = JsonDocument.Parse(VertexAITicketPrompt.ResponseSchemaJson).RootElement;

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = VertexAITicketPrompt.SystemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            inlineData = new
                            {
                                mimeType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType,
                                data = imageBase64
                            }
                        },
                        new { text = VertexAITicketPrompt.UserPrompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _config.VertexAITemperature,
                maxOutputTokens = _config.VertexAIMaxOutputTokens,
                responseMimeType = "application/json",
                responseSchema = schema
            }
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static System.Collections.Generic.List<VertexAIAnalysisResult> ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new UserFriendlyException("Vertex AI không trả về kết quả.", Truncate(responseBody, 500));
        }

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.GetArrayLength() == 0)
        {
            throw new UserFriendlyException("Vertex AI trả về dữ liệu trống.", Truncate(responseBody, 500));
        }

        var text = parts[0].TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new UserFriendlyException("Vertex AI không trả về JSON.", Truncate(responseBody, 500));
        }

        TicketModelContainer payload;
        try
        {
            payload = JsonSerializer.Deserialize<TicketModelContainer>(text,
                          new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new TicketModelContainer();
        }
        catch (JsonException ex)
        {
            throw new UserFriendlyException("Vertex AI trả về JSON không hợp lệ.", ex.Message);
        }

        if (payload.Tickets == null || payload.Tickets.Count == 0)
        {
            return new System.Collections.Generic.List<VertexAIAnalysisResult>();
        }

        return payload.Tickets.Select(t => new VertexAIAnalysisResult
        {
            Province = NullIfBlank(t.Province),
            DrawDate = ParseDate(t.Draw_Date),
            TicketNumber = NullIfBlank(t.Ticket_Number),
            DrawType = NullIfBlank(t.Draw_Type),
            Confidence = t.Confidence.HasValue ? (decimal)Math.Clamp(t.Confidence.Value, 0d, 1d) : (decimal?)null,
            Notes = NullIfBlank(t.Notes),
            RawJson = text
        }).ToList();
    }

    private static DateTime? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var iso))
            return DateTime.SpecifyKind(iso.Date, DateTimeKind.Utc);
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fallback))
            return DateTime.SpecifyKind(fallback.Date, DateTimeKind.Utc);
        return null;
    }

    private static string NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);

    private sealed class TicketModelContainer
    {
        public System.Collections.Generic.List<TicketModelPayload> Tickets { get; set; }
    }

    private sealed class TicketModelPayload
    {
        public string Province { get; set; }
        public string Draw_Date { get; set; }
        public string Ticket_Number { get; set; }
        public string Draw_Type { get; set; }
        public double? Confidence { get; set; }
        public string Notes { get; set; }
    }
}
