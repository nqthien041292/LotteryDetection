using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Configuration;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Tasks;

public class RemoteTaskService : ITaskService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteTaskService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        // Disable auto-redirect so an expired/missing bearer token surfaces as
        // 302 (caught by EnsureSuccessStatusCode) instead of silently following
        // the backend's redirect to a 200 HTML error page.
        _httpClient = httpClient ?? new HttpClient(DevHttpsHelper.CreateHandler());
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<TaskSummary> GetDashboardSummaryAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var metrics = await _httpClient.GetFromJsonAsync<DashboardMetricsResponse>(
                "api/mobile/dashboard/summary", JsonOptions);
            if (metrics == null) return new TaskSummary();

            return new TaskSummary
            {
                OpenTasks = metrics.OpenTasks,
                DueToday = metrics.DueToday,
                Completed = metrics.Completed,
                Highlights = metrics.Highlights
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] GetDashboardSummaryAsync failed: {ex.Message}");
            return new TaskSummary();
        }
    }

    public async Task<IEnumerable<TaskItem>> GetBoardTasksAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<BoardTaskResponse>>(
                "api/mobile/tasks", JsonOptions);
            if (items == null) return Enumerable.Empty<TaskItem>();
            return items.Select(MapBoardTask).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] GetBoardTasksAsync failed: {ex.Message}");
            return Enumerable.Empty<TaskItem>();
        }
    }

    public async Task<IEnumerable<TaskItem>> GetBoardActivityAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<BoardTaskResponse>>(
                "api/mobile/family/board/activity", JsonOptions);
            if (items == null) return Enumerable.Empty<TaskItem>();
            return items.Select(MapBoardTask).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] GetBoardActivityAsync failed: {ex.Message}");
            return Enumerable.Empty<TaskItem>();
        }
    }

    public async Task<TaskItem?> GetTaskByIdAsync(string id)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var item = await _httpClient.GetFromJsonAsync<BoardTaskResponse>(
                $"api/mobile/tasks/{id}", JsonOptions);
            return item == null ? null : MapBoardTask(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] GetTaskByIdAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<TaskItem>> GetRecentActivityAsync()
    {
        return await GetBoardActivityAsync();
    }

    public async Task<TaskItem?> CreateTaskAsync(TaskItem task)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/mobile/tasks", ToContentRequest(task));
            return await ReadTaskResponseAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] CreateTaskAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<TaskItem?> UpdateTaskContentAsync(TaskItem task)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/mobile/tasks/{task.Id}/content", ToContentRequest(task));
            return await ReadTaskResponseAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] UpdateTaskContentAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<TaskItem?> UpdateTaskStatusAsync(string id, string status)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/mobile/tasks/{id}/status", new { Status = status });
            return await ReadTaskResponseAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] UpdateTaskStatusAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<TaskItem?> AssignTaskAsync(string id, string assigneeId, string assigneeName)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/mobile/tasks/{id}/assignee", new { AssigneeId = assigneeId, AssigneeName = assigneeName });
            return await ReadTaskResponseAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] AssignTaskAsync failed: {ex.Message}");
            return null;
        }
    }

    public Task<TaskItem?> MoveTaskAsync(string id, string columnId)
    {
        var status = columnId switch
        {
            "doing" => "InProgress",
            "done" => "Completed",
            _ => "Open"
        };
        return UpdateTaskStatusAsync(id, status);
    }

    public async Task<bool> DeleteTaskAsync(string id)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var mobileStatus = await TryDeleteStatusAsync($"api/mobile/tasks/{id}");
            if (IsSuccessStatusCode(mobileStatus)) return true;

            if (Guid.TryParse(id, out _) && await VoiceTaskExistsAsync(id))
            {
                var voiceStatus = await TryDeleteStatusAsync($"api/voice/recordings/{id}");
                return IsSuccessStatusCode(voiceStatus);
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] DeleteTaskAsync failed: {ex.Message}");
            return false;
        }
    }

    private async Task<HttpStatusCode> TryDeleteStatusAsync(string url)
    {
        Console.WriteLine($"[RemoteTaskService] DeleteTaskAsync - DELETE {url}");
        using var response = await _httpClient.DeleteAsync(url);
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[RemoteTaskService] DeleteTaskAsync - HTTP {(int)response.StatusCode} for {url}: {responseBody}");
        return response.StatusCode;
    }

    private async Task<bool> VoiceTaskExistsAsync(string id)
    {
        var url = $"api/voice/recordings/{id}/preview";
        Console.WriteLine($"[RemoteTaskService] DeleteTaskAsync - VERIFY {url}");
        using var response = await _httpClient.GetAsync(url);
        Console.WriteLine($"[RemoteTaskService] DeleteTaskAsync - VERIFY HTTP {(int)response.StatusCode} for {url}");
        return response.IsSuccessStatusCode;
    }

    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code >= 200 && code <= 299;
    }

    private static object ToContentRequest(TaskItem task)
    {
        return new
        {
            task.Title,
            Notes = task.Description,
            task.DueDate,
            Priority = ToApiPriority(task.Priority),
            task.Category,
            task.AssigneeId,
            AssigneeName = task.Owner
        };
    }

    private async Task<TaskItem?> ReadTaskResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<BoardTaskResponse>(JsonOptions);
        return dto == null ? null : MapBoardTask(dto);
    }

    private async Task EnsureAuthHeaderAsync()
    {
        try
        {
            var token = await _tokenProvider();
            _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Remove("X-User-Timezone");
            _httpClient.DefaultRequestHeaders.Add("X-User-Timezone", TimeZoneInfo.Local.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteTaskService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private static TaskItem MapToTaskItem(FamilyTaskListItemResponse src)
    {
        return new TaskItem
        {
            Id = src.Id.ToString(),
            Title = src.Title ?? string.Empty,
            Description = src.Description ?? string.Empty,
            Owner = src.Owner ?? string.Empty,
            Status = MapStatus(src.Status),
            Priority = MapPriority(src.Priority),
            DueDate = src.DueDate,
            Tags = Array.Empty<string>()
        };
    }

    private static TaskItem MapBoardTask(BoardTaskResponse src)
    {
        return new TaskItem
        {
            Id = src.Id.ToString(),
            Title = src.Title ?? string.Empty,
            Description = src.Notes ?? string.Empty,
            Owner = src.AssigneeName ?? string.Empty,
            AssigneeId = src.AssigneeId ?? string.Empty,
            Status = MapStatus(src.Status),
            Priority = MapPriority(src.Priority),
            Category = src.Category ?? string.Empty,
            DueDate = src.DueDate,
            UpdatedAt = src.UpdatedAt,
            IsPinned = src.IsPinned,
            Tags = Array.Empty<string>()
        };
    }

    private static string MapStatus(string? status) => status switch
    {
        "Open" => "Open",
        "InProgress" => "In progress",
        "Completed" => "Completed",
        "Cancelled" => "Cancelled",
        _ => status ?? "Open"
    };

    private static string MapPriority(string? priority) => priority switch
    {
        "Low" => "Low",
        "Normal" => "Medium",
        "High" => "High",
        _ => priority ?? "Normal"
    };

    private static string ToApiPriority(string? priority) => priority switch
    {
        "Medium" => "Normal",
        _ => priority ?? "Normal"
    };

    private class DashboardMetricsResponse
    {
        public int OpenTasks { get; set; }
        public int DueToday { get; set; }
        public int Completed { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Highlights { get; set; } = new();
    }

    private class FamilyTaskListItemResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Owner { get; set; }
    }

    private class BoardTaskResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public bool IsPinned { get; set; }
    }
}
