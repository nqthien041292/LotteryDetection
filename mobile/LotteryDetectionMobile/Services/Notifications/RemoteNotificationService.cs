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

namespace LotteryDetectionMobile.Services.Notifications;

public class RemoteNotificationService : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteNotificationService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<IEnumerable<NotificationItem>> GetNotificationsAsync()
    {
        await EnsureAuthHeaderAsync();
        var items = new List<NotificationItem>();

        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<NotificationResponse>>(
                "api/mobile/notifications?take=50", JsonOptions);
            if (dtos != null) items.AddRange(dtos.Select(MapToItem));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteNotificationService] GetNotificationsAsync failed: {ex.Message}");
        }

        try
        {
            var conflicts = await _httpClient.GetFromJsonAsync<List<NotificationResponse>>(
                "api/mobile/notifications/conflicts", JsonOptions);
            if (conflicts != null) items.AddRange(conflicts.Select(MapToItem));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteNotificationService] GetConflictsAsync failed: {ex.Message}");
        }

        return items;
    }

    public async Task MarkAsReadAsync(string notificationId)
    {
        if (!Guid.TryParse(notificationId, out var guid)) return;
        await EnsureAuthHeaderAsync();
        try
        {
            await _httpClient.PostAsync($"api/mobile/notifications/{guid}/read", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteNotificationService] MarkAsReadAsync failed: {ex.Message}");
        }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<int>("api/mobile/notifications/unread-count", JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteNotificationService] GetUnreadCountAsync failed: {ex.Message}");
            return 0;
        }
    }

    private static NotificationItem MapToItem(NotificationResponse d)
    {
        var (icon, iconBg) = ResolveIcon(d.Category, d.Severity);
        var elapsed = DateTime.UtcNow - d.Timestamp;
        var subtitle = elapsed.TotalMinutes < 60
            ? $"{(int)elapsed.TotalMinutes}m ago"
            : elapsed.TotalHours < 24
                ? $"{(int)elapsed.TotalHours}h ago"
                : $"{(int)elapsed.TotalDays}d ago";

        return new NotificationItem
        {
            Id = d.Id,
            Title = d.Title,
            Message = d.Message,
            Subtitle = subtitle,
            Timestamp = d.Timestamp,
            IsUnread = d.IsUnread,
            Category = d.Category,
            Icon = icon,
            IconBackground = iconBg
        };
    }

    private static (string Icon, string IconBackground) ResolveIcon(string category, string severity)
    {
        return (category.ToLowerInvariant(), severity.ToLowerInvariant()) switch
        {
            ("task", "success") => ("✅", "#DCFCE7"),
            ("task", "warn") or ("task", "warning") => ("⚠️", "#FEF3C7"),
            ("task", "error") or ("task", "fatal") => ("🚨", "#FEE2E2"),
            ("task", _) => ("📋", "#DBEAFE"),
            ("calendar", _) => ("📅", "#DBEAFE"),
            ("conflict", _) => ("⚡", "#FEF3C7"),
            ("assistant", "success") => ("🤖", "#DCFCE7"),
            ("assistant", _) => ("🤖", "#F3E8FF"),
            _ => ("🔔", "#E5E7EB")
        };
    }

    private async Task EnsureAuthHeaderAsync()
    {
        try
        {
            var token = await _tokenProvider();
            _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteNotificationService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private class NotificationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsUnread { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }
}
