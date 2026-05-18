using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LotteryDetectionMobile.Models.Voice;
using LotteryDetectionMobile.Services.Logging;

namespace LotteryDetectionMobile.Services.Voice;

public class VoiceUploadApiClient
{
    /// <summary>
    ///     Case-insensitive JSON options for ABP camelCase responses.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Func<Task<string?>>? _tokenProvider;
    private readonly HttpClient httpClient;

    public VoiceUploadApiClient(HttpClient? httpClient = null, VoiceApiOptions? options = null)
    {
        options ??= new VoiceApiOptions();
        // Disable auto-redirect so an expired/missing bearer token surfaces as
        // 302 (caught by EnsureSuccessStatusCode) instead of silently following
        // the backend's redirect to a 200 HTML error page.
        this.httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        this.httpClient.BaseAddress = options.BaseUri;

        Console.WriteLine($"[VoiceUploadApi] Initialized with BaseAddress: {this.httpClient.BaseAddress}");

        // Set timeout to prevent hanging requests
        this.httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Store token provider for lazy evaluation on each request
        _tokenProvider = options.GetBearerTokenAsync;
    }

    private async Task EnsureAuthHeaderAsync()
    {
        var log = RemoteLogService.Instance;

        if (_tokenProvider != null)
            try
            {
                log.Debug("VoiceUpload", "Getting auth token from provider...");
                var token = await _tokenProvider();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    log.Info("VoiceUpload", $"Auth header set, token length: {token.Length}");
                }
                else
                {
                    // Clear stale auth header to avoid sending expired tokens
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    log.Warn("VoiceUpload",
                        "Token provider returned null/empty token - requests will likely fail with 401");
                }

                httpClient.DefaultRequestHeaders.Remove("X-User-Timezone");
                httpClient.DefaultRequestHeaders.Add("X-User-Timezone", TimeZoneInfo.Local.Id);
            }
            catch (Exception ex)
            {
                // Clear stale auth header on error
                httpClient.DefaultRequestHeaders.Authorization = null;
                log.Error("VoiceUpload", "Failed to get auth token", ex);
            }
        else
            log.Warn("VoiceUpload", "No token provider configured - requests will likely fail with 401");
    }

    public async Task<StartVoiceUploadResult> StartAsync(CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        log.Info("VoiceUpload", $"StartAsync - calling POST {httpClient.BaseAddress}api/voice/recordings/start");

        await EnsureAuthHeaderAsync();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/voice/recordings/start");
            log.Debug("VoiceUpload",
                $"Request headers: Auth={httpClient.DefaultRequestHeaders.Authorization?.Scheme ?? "none"}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            log.Info("VoiceUpload", $"StartAsync - response: {(int)response.StatusCode} {response.StatusCode}",
                new Dictionary<string, string>
                    { { "responseBody", responseBody.Length > 500 ? responseBody[..500] : responseBody } });

            if (!response.IsSuccessStatusCode)
            {
                log.Error("VoiceUpload", $"StartAsync FAILED: {response.StatusCode} - {responseBody}");
                response.EnsureSuccessStatusCode();
            }

            var result = await ReadWrappedAsync<StartVoiceUploadResult>(response, cancellationToken);
            if (result == null)
            {
                log.Error("VoiceUpload", "StartAsync - Failed to parse response, result is null");
                throw new InvalidOperationException("Failed to start voice upload session.");
            }

            log.Info("VoiceUpload", $"StartAsync SUCCESS - sessionId: {result.SessionId}");
            return result;
        }
        catch (HttpRequestException ex)
        {
            log.Error("VoiceUpload", $"StartAsync - Network error: {ex.Message}", ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            log.Error("VoiceUpload", "StartAsync - Request timed out or cancelled", ex);
            throw;
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"StartAsync - Unexpected error: {ex.Message}", ex);
            throw;
        }
    }

    public async Task UploadChunkAsync(Guid sessionId, Stream stream, string contentType,
        CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        log.Info("VoiceUpload",
            $"UploadChunkAsync - sessionId: {sessionId}, streamLength: {stream.Length}, contentType: {contentType}");

        await EnsureAuthHeaderAsync();

        try
        {
            using var form = new MultipartFormDataContent();
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType);

            form.Add(streamContent, "chunk", $"audio_{sessionId}.m4a");

            var url = $"api/voice/recordings/{sessionId}/chunk";
            log.Debug("VoiceUpload", $"Uploading to: {url}");

            using var response = await httpClient.PostAsync(url, form, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            log.Info("VoiceUpload", $"UploadChunkAsync - response: {(int)response.StatusCode}",
                new Dictionary<string, string> { { "responseBody", responseBody } });

            if (!response.IsSuccessStatusCode)
                log.Error("VoiceUpload", $"UploadChunkAsync FAILED: {response.StatusCode} - {responseBody}");

            response.EnsureSuccessStatusCode();
            log.Info("VoiceUpload", "UploadChunkAsync SUCCESS");
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"UploadChunkAsync - ERROR: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<CompleteVoiceUploadResult> CompleteAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await CompleteAsync(sessionId, null, cancellationToken);
    }

    public async Task<CompleteVoiceUploadResult> CompleteAsync(Guid sessionId, string? connectionId,
        CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;

        var url = $"api/voice/recordings/{sessionId}/complete";
        if (!string.IsNullOrEmpty(connectionId)) url += $"?connectionId={Uri.EscapeDataString(connectionId)}";

        log.Info("VoiceUpload", $"CompleteAsync - POST {url}");

        try
        {
            return await WithRetryAsync(async () =>
            {
                await EnsureAuthHeaderAsync();
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                using var response = await httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                log.Info("VoiceUpload", $"CompleteAsync - response: {(int)response.StatusCode}",
                    new Dictionary<string, string>
                        { { "responseBody", responseBody.Length > 500 ? responseBody[..500] : responseBody } });

                if (!response.IsSuccessStatusCode)
                    log.Error("VoiceUpload", $"CompleteAsync FAILED: {response.StatusCode} - {responseBody}");

                response.EnsureSuccessStatusCode();

                var result = await ReadWrappedAsync<CompleteVoiceUploadResult>(response, cancellationToken);
                if (result == null)
                {
                    log.Error("VoiceUpload", "CompleteAsync - Failed to parse response, result is null");
                    throw new InvalidOperationException("Failed to complete voice upload session.");
                }

                log.Info("VoiceUpload",
                    $"CompleteAsync SUCCESS - filePath: {result.FilePath}, sessionId: {result.SessionId}, voiceTaskId: {result.VoiceTaskId}");
                return result;
            }, "CompleteAsync");
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"CompleteAsync - ERROR: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    ///     Confirms a voice task for creation in Microsoft 365.
    ///     Called when user clicks "Save As Task" after reviewing the transcript.
    /// </summary>
    public async Task<ConfirmTaskResult> ConfirmTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        var url = $"api/voice/recordings/{taskId}/confirm";

        log.Info("VoiceUpload", $"ConfirmTaskAsync - POST {url}");

        try
        {
            return await WithRetryAsync(async () =>
            {
                await EnsureAuthHeaderAsync();
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                using var response = await httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                log.Info("VoiceUpload", $"ConfirmTaskAsync - response: {(int)response.StatusCode}",
                    new Dictionary<string, string> { { "responseBody", responseBody } });

                if (!response.IsSuccessStatusCode)
                {
                    log.Error("VoiceUpload", $"ConfirmTaskAsync FAILED: {response.StatusCode} - {responseBody}");
                    response.EnsureSuccessStatusCode();
                }

                var result = await ReadWrappedAsync<ConfirmTaskResult>(response, cancellationToken);
                if (result == null)
                {
                    log.Error("VoiceUpload", "ConfirmTaskAsync - Failed to parse response, result is null");
                    throw new InvalidOperationException("Failed to confirm task.");
                }

                log.Info("VoiceUpload", $"ConfirmTaskAsync SUCCESS - taskId: {result.TaskId}, status: {result.Status}");
                return result;
            }, "ConfirmTaskAsync");
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"ConfirmTaskAsync - ERROR: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    ///     Gets task preview with AI-extracted entities.
    ///     Mobile polls this after upload until status is TranscriptionCompleted.
    /// </summary>
    public async Task<VoiceTaskPreview> GetPreviewAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        var url = $"api/voice/recordings/{taskId}/preview";

        log.Debug("VoiceUpload", $"GetPreviewAsync - GET {url}");

        await EnsureAuthHeaderAsync();

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            log.Info("VoiceUpload",
                $"GetPreviewAsync - HTTP {(int)response.StatusCode}, rawJson: {(rawJson?.Length > 800 ? rawJson[..800] : rawJson)}");

            if (!response.IsSuccessStatusCode)
            {
                log.Warn("VoiceUpload", $"GetPreviewAsync - {response.StatusCode}");
                return null;
            }

            // Deserialize ABP-wrapped response manually (content already read above)
            VoiceTaskPreview result = null;
            try
            {
                var wrapped = JsonSerializer.Deserialize<AbpResponse<VoiceTaskPreview>>(rawJson, JsonOptions);
                if (wrapped?.Result != null)
                    result = wrapped.Result;
                else
                    result = JsonSerializer.Deserialize<VoiceTaskPreview>(rawJson, JsonOptions);
            }
            catch (JsonException jex)
            {
                log.Error("VoiceUpload", $"GetPreviewAsync - JSON parse error: {jex.Message}", jex);
            }

            log.Info("VoiceUpload",
                $"GetPreviewAsync - parsed: status={result?.Status}, title={result?.Title}, assignee={result?.Assignee}, transcript={result?.Transcript?.Length ?? 0} chars, dueDateTime={result?.DueDateTime}");
            return result;
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"GetPreviewAsync - ERROR: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    ///     Gets the current user's saved voice tasks for the My Tasks page.
    /// </summary>
    public async Task<PagedVoiceTaskListResult> GetMyTasksAsync(int skip = 0, int take = 20,
        CancellationToken cancellationToken = default)
    {
        var log = RemoteLogService.Instance;
        var url = $"api/voice/recordings/my-tasks?skip={skip}&take={take}";

        log.Debug("VoiceUpload", $"GetMyTasksAsync - GET {url}");
        await EnsureAuthHeaderAsync();

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            log.Info("VoiceUpload", $"GetMyTasksAsync - HTTP {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                log.Warn("VoiceUpload", $"GetMyTasksAsync - {response.StatusCode}");
                return new PagedVoiceTaskListResult();
            }

            // Deserialize ABP-wrapped response
            var wrapped = JsonSerializer.Deserialize<AbpResponse<PagedVoiceTaskListResult>>(rawJson, JsonOptions);
            if (wrapped?.Result != null) return wrapped.Result;

            var direct = JsonSerializer.Deserialize<PagedVoiceTaskListResult>(rawJson, JsonOptions);
            return direct ?? new PagedVoiceTaskListResult();
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"GetMyTasksAsync - ERROR: {ex.Message}", ex);
            return new PagedVoiceTaskListResult();
        }
    }

    /// <summary>
    ///     Updates a voice task's status (e.g., mark as Completed).
    /// </summary>
    public async Task<bool> UpdateTaskStatusAsync(Guid taskId, string status,
        CancellationToken cancellationToken = default)
    {
        var log = RemoteLogService.Instance;
        var url = $"api/voice/recordings/{taskId}/status";

        log.Info("VoiceUpload", $"UpdateTaskStatusAsync - PATCH {url} status={status}");
        await EnsureAuthHeaderAsync();

        try
        {
            var content = JsonContent.Create(new { Status = status });
            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            log.Info("VoiceUpload", $"UpdateTaskStatusAsync - HTTP {(int)response.StatusCode}",
                new Dictionary<string, string> { { "responseBody", responseBody } });

            if (!response.IsSuccessStatusCode)
            {
                log.Error("VoiceUpload", $"UpdateTaskStatusAsync FAILED: {response.StatusCode} - {responseBody}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"UpdateTaskStatusAsync - ERROR: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    ///     Updates task content fields via PATCH /api/voice/recordings/{taskId}/content
    /// </summary>
    public async Task<VoiceTaskPreview?> UpdateTaskContentAsync(Guid taskId, UpdateTaskContentRequest request,
        CancellationToken cancellationToken = default)
    {
        var log = RemoteLogService.Instance;
        var url = $"api/voice/recordings/{taskId}/content";

        log.Info("VoiceUpload", $"UpdateTaskContentAsync - PATCH {url}");
        await EnsureAuthHeaderAsync();

        try
        {
            var content = JsonContent.Create(request);
            using var httpRequest = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            log.Info("VoiceUpload", $"UpdateTaskContentAsync - HTTP {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                log.Error("VoiceUpload", $"UpdateTaskContentAsync FAILED: {response.StatusCode} - {responseBody}");
                return null;
            }

            // ABP wraps responses as { result: { ... } } — unwrap first, fallback to direct
            var wrapped = JsonSerializer.Deserialize<AbpResponse<VoiceTaskPreview>>(responseBody, JsonOptions);
            if (wrapped?.Result != null) return wrapped.Result;
            return JsonSerializer.Deserialize<VoiceTaskPreview>(responseBody, JsonOptions);
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"UpdateTaskContentAsync - ERROR: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    ///     Deletes a voice task and its linked family task.
    /// </summary>
    public async Task<bool> DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var log = RemoteLogService.Instance;
        var url = $"api/voice/recordings/{taskId}";

        log.Info("VoiceUpload", $"DeleteTaskAsync - DELETE {url}");
        await EnsureAuthHeaderAsync();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            log.Info("VoiceUpload", $"DeleteTaskAsync - HTTP {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                log.Error("VoiceUpload", $"DeleteTaskAsync FAILED: {response.StatusCode} - {responseBody}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            log.Error("VoiceUpload", $"DeleteTaskAsync - ERROR: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    ///     Retries an HTTP operation with exponential backoff for transient failures.
    /// </summary>
    private async Task<T> WithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        var log = RemoteLogService.Instance;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < maxRetries && IsTransient(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                log.Warn("VoiceUpload",
                    $"{operationName} - transient error (attempt {attempt}/{maxRetries}), retrying in {delay.TotalSeconds}s: {ex.Message}");
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex) when
                (attempt < maxRetries && !ex.CancellationToken.IsCancellationRequested)
            {
                // Timeout (not user cancellation)
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                log.Warn("VoiceUpload",
                    $"{operationName} - timeout (attempt {attempt}/{maxRetries}), retrying in {delay.TotalSeconds}s");
                await Task.Delay(delay);
            }

        // Final attempt - let exception propagate
        return await operation();
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        if (ex.StatusCode == null) return true; // Network error
        var code = (int)ex.StatusCode;
        return code == 408 || code == 429 || code == 502 || code == 503 || code == 504;
    }

    private static async Task<T?> ReadWrappedAsync<T>(HttpResponseMessage response, CancellationToken token)
        where T : class
    {
        // ABP wraps responses as { result, targetUrl, success, error, unAuthorizedRequest, __abp }
        // ABP serializes property names as camelCase, so we need case-insensitive deserialization.
        var json = await response.Content.ReadAsStringAsync(token);
        var wrapped = JsonSerializer.Deserialize<AbpResponse<T>>(json, JsonOptions);
        if (wrapped?.Result != null) return wrapped.Result;

        // Fallback: try to read as plain DTO
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private class AbpResponse<T>
    {
        [JsonPropertyName("result")] public T? Result { get; set; }
    }
}
