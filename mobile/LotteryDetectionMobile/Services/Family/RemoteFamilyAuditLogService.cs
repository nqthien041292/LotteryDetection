using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Family;

public class RemoteFamilyAuditLogService : IFamilyAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteFamilyAuditLogService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<IEnumerable<AuditEntry>> GetAuditLogAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<AuditLogResponse>>(
                "api/mobile/family/members/audit-log?take=20", JsonOptions);
            if (dtos == null) return Enumerable.Empty<AuditEntry>();
            return dtos.Select(MapToEntry).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyAuditLogService] GetAuditLogAsync failed: {ex.Message}");
            return Enumerable.Empty<AuditEntry>();
        }
    }

    private static AuditEntry MapToEntry(AuditLogResponse r) => new()
    {
        IconGlyph = r.Action switch
        {
            "RoleChanged" => "↑",
            "Invited"     => "✉",
            "Accepted"    => "✓",
            "Removed"     => "✕",
            _             => "•"
        },
        TintColor = r.Action switch
        {
            "RoleChanged" => "#DCFCE7",
            "Invited"     => "#E0EAFF",
            "Accepted"    => "#D1F0EC",
            "Removed"     => "#FFE4DD",
            _             => "#F3F4F6"
        },
        ForegroundColor = r.Action switch
        {
            "RoleChanged" => "#15803D",
            "Invited"     => "#1E40AF",
            "Accepted"    => "#115E59",
            "Removed"     => "#B23A1A",
            _             => "#6B7280"
        },
        What = r.Action switch
        {
            "RoleChanged" => $"Changed {r.TargetDisplayName} to {r.NewRole}",
            "Invited"     => $"Invited {r.TargetDisplayName}",
            "Accepted"    => $"{r.TargetDisplayName} accepted invite",
            "Removed"     => $"Removed {r.TargetDisplayName}",
            _             => r.Action
        },
        Who = FormatWhen(r.OccurredAt)
    };

    private static string FormatWhen(DateTime occurredAt)
    {
        var elapsed = DateTime.UtcNow - occurredAt;
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
        return occurredAt.ToString("MMM d");
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
            Console.WriteLine($"[RemoteFamilyAuditLogService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private class AuditLogResponse
    {
        public Guid Id { get; set; }
        public string ActorDisplayName { get; set; } = string.Empty;
        public string TargetDisplayName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string OldRole { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
