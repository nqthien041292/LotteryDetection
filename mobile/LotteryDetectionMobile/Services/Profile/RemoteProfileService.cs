using System.Net.Http.Headers;
using System.Net.Http.Json;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Profile;

public class RemoteProfileService : IProfileService
{
    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteProfileService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task UpdateDisplayNameAsync(string displayName)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                "api/mobile/profile/display-name",
                new { DisplayName = displayName });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteProfileService] UpdateDisplayNameAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task<string?> GetDisplayNameAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.GetAsync("api/mobile/profile/display-name");
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<DisplayNameResponse>();
            return string.IsNullOrWhiteSpace(payload?.DisplayName) ? null : payload!.DisplayName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteProfileService] GetDisplayNameAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<UserPreferencesData?> GetPreferencesAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<UserPreferencesData>(
                "api/mobile/profile/preferences", JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteProfileService] GetPreferencesAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task SavePreferencesAsync(UserPreferencesData prefs)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                "api/mobile/profile/preferences", prefs, JsonOptions);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteProfileService] SavePreferencesAsync failed: {ex.Message}");
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class DisplayNameResponse
    {
        public string? DisplayName { get; set; }
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
            Console.WriteLine($"[RemoteProfileService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
