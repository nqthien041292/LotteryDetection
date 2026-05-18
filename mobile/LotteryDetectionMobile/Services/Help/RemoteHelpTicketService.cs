using System.Net.Http.Headers;
using System.Net.Http.Json;
using LotteryDetectionMobile.Models.Help;
using LotteryDetectionMobile.Services.Configuration;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Help;

public class RemoteHelpTicketService : IHelpTicketService
{
    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteHelpTicketService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(DevHttpsHelper.CreateHandler());
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<bool> SubmitAsync(HelpTicket ticket)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/mobile/help/tickets",
                new { ticket.Topic, ticket.Message });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteHelpTicketService] SubmitAsync failed: {ex.Message}");
            return false;
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
        catch
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
