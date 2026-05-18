using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Configuration;

namespace LotteryDetectionMobile.Services.Voice;

public class VoiceApiOptions
{
    private readonly IAuthService? _authService;

    public VoiceApiOptions()
    {
    }

    public VoiceApiOptions(IAuthService? authService)
    {
        _authService = authService;
    }

    public string BaseUrl { get; set; } = AppConfiguration.GetVoiceApiBaseUrl();

    [Obsolete("Use GetBearerTokenAsync() instead. Direct property access doesn't support async token refresh.")]
    public string? BearerToken { get; set; }

    public Uri BaseUri => new(BaseUrl, UriKind.Absolute);

    /// <summary>
    ///     Maximum consecutive chunk failures before falling back to file upload.
    /// </summary>
    public int MaxChunkFailures { get; set; } = 5;

    /// <summary>
    ///     Whether real-time streaming is enabled (can be disabled for testing).
    /// </summary>
    public bool EnableRealtimeStreaming { get; set; } = true;

    /// <summary>
    ///     Gets the current bearer token, refreshing if needed.
    ///     Returns null if auth service unavailable or user not signed in.
    /// </summary>
    public async Task<string?> GetBearerTokenAsync()
    {
        Console.WriteLine(
            $"[VoiceApiOptions] GetBearerTokenAsync - AuthService={(_authService != null ? "present" : "null")}, IsSignedIn={_authService?.IsSignedIn}");

        if (_authService == null)
        {
            Console.WriteLine("[VoiceApiOptions] No auth service - returning fallback token");
#pragma warning disable CS0618 // Type or member is obsolete
            return BearerToken;
#pragma warning restore CS0618
        }

        try
        {
            // Always try to get token - IsSignedIn may be stale
            var token = await _authService.GetAccessTokenAsync();
            Console.WriteLine($"[VoiceApiOptions] Got token from auth service, length={token?.Length ?? 0}");
            return token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VoiceApiOptions] Error getting token: {ex.Message}");
#pragma warning disable CS0618 // Type or member is obsolete
            return BearerToken; // Fallback on error
#pragma warning restore CS0618
        }
    }
}