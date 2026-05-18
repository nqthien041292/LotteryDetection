using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.AIAssistant;

public class RemoteAIService : IAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteAIService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        // Disable auto-redirect: when the bearer token is missing or expired, the
        // ABP backend responds with 302 → /Error?statusCode=401 (an HTML page,
        // status 200). Following that redirect swallows the real auth failure
        // and produces a JSON-parse error downstream. With AllowAutoRedirect=false
        // the 302 stays as 302 and EnsureSuccessStatusCode surfaces it cleanly.
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // OpenAI can be slow
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<string> BuildTaskPreviewAsync(string transcript)
    {
        // Phase 7 explicitly defers unstructured previews for MVP, keeping this as client-side stub
        return await Task.FromResult($"Task captured: {transcript}\n(Preview generation deferred)");
    }

    public async Task<IEnumerable<string>> GetPromptSuggestionsAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var suggestions = await _httpClient.GetFromJsonAsync<List<string>>(
                "api/mobile/ai/suggestions", JsonOptions);
            return suggestions ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteAIService] GetPromptSuggestionsAsync failed: {ex.Message}");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<AssistantSuggestion>> GetSuggestionsAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<AssistantSuggestionResponse>>(
                "api/mobile/ai/suggestions/smart", JsonOptions);
            if (dtos == null || dtos.Count == 0)
                return Enumerable.Empty<AssistantSuggestion>();

            return dtos.Select(d => new AssistantSuggestion
            {
                Id = d.Id ?? Guid.NewGuid().ToString(),
                Kind = d.Kind ?? "create",
                Title = d.Title ?? string.Empty,
                Reason = d.Reason ?? string.Empty,
                Member = d.Member ?? "home",
                MemberName = d.MemberName ?? "Shared",
                Priority = d.Priority ?? "med",
                IsConflict = d.IsConflict
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteAIService] GetSuggestionsAsync failed: {ex.Message}");
            return Enumerable.Empty<AssistantSuggestion>();
        }
    }

    public async Task<IEnumerable<TaskItem>> GetAssistantDraftTasksAsync(string context)
    {
        // Let HTTP / network / auth failures propagate so callers can show a
        // "couldn't reach the assistant" message instead of conflating them
        // with a successful-but-empty draft list ("no tasks identified").
        var hasToken = await EnsureAuthHeaderAsync();
        if (!hasToken)
            throw new UnauthorizedAccessException("No bearer token available — user is not signed in.");

        var request = new { Prompt = context };
        var response = await _httpClient.PostAsJsonAsync("api/mobile/ai/tasks/draft", request);
        response.EnsureSuccessStatusCode();

        // Backend redirect-to-error-page can still slip through if AutoRedirect
        // ever gets re-enabled or if a future middleware returns HTML on 200.
        // Reject anything that isn't JSON instead of letting ReadFromJsonAsync
        // throw a cryptic parser exception.
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException(
                $"Unexpected response content-type '{contentType}' from {response.RequestMessage?.RequestUri} (status {(int)response.StatusCode}).");

        var drafts = await response.Content.ReadFromJsonAsync<List<TaskDraftResponse>>(JsonOptions);
        if (drafts == null) return Enumerable.Empty<TaskItem>();

        return drafts.Select(d => new TaskItem
        {
            Title = d.Title ?? string.Empty,
            Description = d.Description ?? string.Empty,
            Owner = d.Owner ?? string.Empty,
            DueDate = d.DueDate,
            Priority = d.Priority ?? "Medium",
            Status = "Draft",
            Tags = string.IsNullOrEmpty(d.Category) ? Array.Empty<string>() : new[] { d.Category },
            Points = d.Points > 0 ? d.Points : 15,
            IsSelected = false
        }).ToList();
    }

    public Task<string> SummarizeChatAsync(string context)
    {
        // YAGNI for Phase 7 MVP: The mock summarized the chat text, but the real flow 
        // immediately creates draft tasks. We just return a static transitioning string.
        return Task.FromResult("I've analyzed your request. Here are some tasks we can add:");
    }

    public async Task<IReadOnlyList<Guid>> ConfirmDraftTasksAsync(IEnumerable<TaskItem> drafts)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var requestDrafts = drafts.Select(d => new TaskDraftResponse
            {
                Title = d.Title,
                Description = d.Description,
                Owner = d.Owner,
                DueDate = d.DueDate,
                Priority = d.Priority,
                Category = d.Tags?.FirstOrDefault(),
                Points = d.Points
            }).ToList();

            var request = new { Drafts = requestDrafts };
            var response = await _httpClient.PostAsJsonAsync("api/mobile/ai/tasks/confirm", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ConfirmTasksResponse>(JsonOptions);
            return result?.CreatedIds ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteAIService] ConfirmDraftTasksAsync failed: {ex.Message}");
            return Array.Empty<Guid>();
        }
    }

    private async Task<bool> EnsureAuthHeaderAsync()
    {
        try
        {
            var token = await _tokenProvider();
            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return false;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteAIService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return false;
        }
    }

    private class AssistantSuggestionResponse
    {
        public string? Id { get; set; }
        public string? Kind { get; set; }
        public string? Title { get; set; }
        public string? Reason { get; set; }
        public string? Member { get; set; }
        public string? MemberName { get; set; }
        public string? Priority { get; set; }
        public bool IsConflict { get; set; }
    }

    private class TaskDraftResponse
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Owner { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public int Points { get; set; }
    }

    private class ConfirmTasksResponse
    {
        public List<Guid> CreatedIds { get; set; } = new();
    }
}
