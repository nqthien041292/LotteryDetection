using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Configuration;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Calendar;

public class RemoteCalendarService : ICalendarService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteCalendarService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(DevHttpsHelper.CreateHandler());
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetFromJsonAsync<List<CalendarEventResponse>>(
                $"api/mobile/calendar/events?from={today}&days=14", JsonOptions);

            if (response == null) return Enumerable.Empty<CalendarEvent>();

            return response.Select(src => new CalendarEvent
            {
                Id = src.Id.ToString(),
                Title = src.Title ?? string.Empty,
                Owner = src.Owner ?? string.Empty,
                Member = string.IsNullOrEmpty(src.Member) ? "home" : src.Member,
                Start = src.Start.ToLocalTime(),
                End = src.End?.ToLocalTime(),
                Status = src.Status ?? "Scheduled",
                Location = src.Location ?? string.Empty,
                Color = string.IsNullOrEmpty(src.Color) ? "#6C63FF" : src.Color
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteCalendarService] GetUpcomingEventsAsync failed: {ex.Message}");
            return Enumerable.Empty<CalendarEvent>();
        }
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
            Console.WriteLine($"[RemoteCalendarService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private class CalendarEventResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Owner { get; set; }
        public string? Member { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string? Status { get; set; }
        public string? Location { get; set; }
        public string? Color { get; set; }
    }
}
