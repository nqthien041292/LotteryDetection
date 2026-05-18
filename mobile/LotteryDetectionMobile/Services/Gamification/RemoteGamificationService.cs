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

namespace LotteryDetectionMobile.Services.Gamification;

public class RemoteGamificationService : IGamificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;
    private StatsResponse? _cachedStats;
    private DateTime _cachedStatsAt = DateTime.MinValue;

    public RemoteGamificationService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<int> GetCurrentPointsAsync()
    {
        var stats = await GetStatsCachedAsync();
        return stats?.TotalPoints ?? 0;
    }

    public async Task<int> GetWeeklyPointsAsync()
    {
        var stats = await GetStatsCachedAsync();
        return stats?.WeeklyPoints ?? 0;
    }

    public async Task<int> GetAvailablePointsAsync()
    {
        var stats = await GetStatsCachedAsync();
        return stats?.AvailablePoints ?? 0;
    }

    public async Task<Streak> GetCurrentStreakAsync()
    {
        var stats = await GetStatsCachedAsync();
        return new Streak { Label = "Consistency", Days = stats?.CurrentStreak ?? 0 };
    }

    public async Task<IEnumerable<Badge>> GetBadgesAsync()
    {
        var stats = await GetStatsCachedAsync();
        if (stats?.Badges == null) return Enumerable.Empty<Badge>();
        return stats.Badges.Select(b => new Badge
        {
            Id = b.Id,
            Title = b.Title,
            Description = b.Description,
            Icon = b.Icon,
            Points = b.Points,
            IsUnlocked = b.IsUnlocked
        }).ToList();
    }

    public async Task<IEnumerable<FamilyMember>> GetLeaderboardAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var entries = await _httpClient.GetFromJsonAsync<List<LeaderboardEntryResponse>>(
                "api/mobile/gamification/leaderboard", JsonOptions);
            if (entries == null) return Enumerable.Empty<FamilyMember>();
            return entries.Select(e => new FamilyMember
            {
                Id = e.UserIdString,
                Name = e.DisplayName,
                Points = e.Points,
                WeeklyPoints = e.WeeklyPoints,
                Streak = e.Streak,
                IsOnline = false,
                Role = "Member"
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteGamificationService] GetLeaderboardAsync failed: {ex.Message}");
            return Enumerable.Empty<FamilyMember>();
        }
    }

    private async Task<StatsResponse?> GetStatsCachedAsync()
    {
        if (_cachedStats != null && DateTime.UtcNow - _cachedStatsAt < CacheTtl)
            return _cachedStats;

        await EnsureAuthHeaderAsync();
        try
        {
            _cachedStats = await _httpClient.GetFromJsonAsync<StatsResponse>(
                "api/mobile/gamification/stats", JsonOptions);
            _cachedStatsAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteGamificationService] GetStatsCachedAsync failed: {ex.Message}");
        }
        return _cachedStats;
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
            Console.WriteLine($"[RemoteGamificationService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private class StatsResponse
    {
        public int TotalPoints { get; set; }
        public int WeeklyPoints { get; set; }
        public int AvailablePoints { get; set; }
        public int CurrentStreak { get; set; }
        public int Level { get; set; }
        public int CompletedTaskCount { get; set; }
        public List<BadgeResponse> Badges { get; set; } = new();
    }

    private class BadgeResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Points { get; set; }
        public bool IsUnlocked { get; set; }
    }

    private class LeaderboardEntryResponse
    {
        public string UserIdString { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int WeeklyPoints { get; set; }
        public int Streak { get; set; }
        public int Rank { get; set; }
        public bool IsMe { get; set; }
    }
}
