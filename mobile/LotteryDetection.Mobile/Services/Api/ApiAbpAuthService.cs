using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LotteryDetection.Mobile.Services.Auth;

namespace LotteryDetection.Mobile.Services.Api;

/// <summary>
///     Real <see cref="IAuthService" /> backed by ABP's TokenAuth controller
///     (POST /api/TokenAuth/Authenticate). Tokens are persisted in
///     <see cref="SecureStorage" /> so the session survives app restarts.
/// </summary>
public class ApiAbpAuthService : IAuthService
{
    private const string AuthPath = "api/TokenAuth/Authenticate";
    private const string ExternalAuthPath = "api/TokenAuth/ExternalAuthenticate";
    private const string RefreshPath = "api/TokenAuth/RefreshToken";

    private const string KeyAccessToken = "lottery.auth.access_token";
    private const string KeyRefreshToken = "lottery.auth.refresh_token";
    private const string KeyExpiresAt = "lottery.auth.expires_at";
    private const string KeyUserName = "lottery.auth.username";

    // Refresh proactively if the token has less than this remaining.
    private static readonly TimeSpan RefreshLeeway = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt;
    private string? _userName;

    public ApiAbpAuthService(HttpClient http)
    {
        _http = http;
    }

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow;

    public string? UserDisplayName => _userName;

    public string? UserEmail => _userName;

    public async Task<string> SignInAsync()
    {
        // The existing LoginPage triggers a parameterless SignInAsync (originally
        // designed for Entra ID redirect). Prompt the user for credentials here
        // so we don't need a full ABP login screen yet.
        var page = Application.Current?.Windows.FirstOrDefault()?.Page
                   ?? throw new InvalidOperationException("No page to host login prompt.");

        var username = await page.DisplayPromptAsync(
            "Đăng nhập", "Tên người dùng hoặc email", "Tiếp", "Huỷ",
            "admin", maxLength: 64, keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(username))
            throw new OperationCanceledException("Người dùng huỷ đăng nhập.");

        var password = await page.DisplayPromptAsync(
            "Đăng nhập", "Mật khẩu", "Đăng nhập", "Huỷ",
            placeholder: "******", maxLength: 128, keyboard: Keyboard.Default);
        if (string.IsNullOrWhiteSpace(password))
            throw new OperationCanceledException("Người dùng huỷ đăng nhập.");

        return await SignInWithCredentialsAsync(username, password);
    }

    public async Task SetDisplayNameAsync(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return;
        _userName = displayName;
        try
        {
            await SecureStorage.SetAsync(KeyUserName, displayName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiAbpAuthService] SetDisplayNameAsync persist failed: {ex.Message}");
        }
    }

    public async Task<string> SignInExternalAsync(string provider, string providerKey, string providerAccessCode, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new ArgumentException("providerKey is required.", nameof(providerKey));
        if (string.IsNullOrWhiteSpace(providerAccessCode))
            throw new ArgumentException("providerAccessCode is required.", nameof(providerAccessCode));

        using var response = await _http.PostAsJsonAsync(ExternalAuthPath, new
        {
            authProvider = provider,
            providerKey,
            providerAccessCode,
            singleSignIn = false
        }, JsonOptions);

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(ExtractError(body) ?? $"HTTP {(int)response.StatusCode}");

        var auth = ParseAuthEnvelope(body)
                   ?? throw new InvalidOperationException("Phản hồi đăng nhập rỗng.");

        var persistedName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : $"{provider}:external";
        await PersistAsync(auth, persistedName);
        return _accessToken!;
    }

    public async Task<string> SignInWithCredentialsAsync(string username, string password)
    {
        using var response = await _http.PostAsJsonAsync(AuthPath, new
        {
            userNameOrEmailAddress = username,
            password,
            rememberClient = true
        }, JsonOptions);

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(ExtractError(body) ?? $"HTTP {(int)response.StatusCode}");

        var auth = ParseAuthEnvelope(body)
                   ?? throw new InvalidOperationException("Phản hồi đăng nhập rỗng.");

        await PersistAsync(auth, username);
        return _accessToken!;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt - RefreshLeeway > DateTimeOffset.UtcNow)
            return _accessToken!;

        if (string.IsNullOrWhiteSpace(_refreshToken))
            throw new InvalidOperationException("Chưa đăng nhập.");

        await _refreshLock.WaitAsync();
        try
        {
            // Re-check inside the lock — another caller may have refreshed already.
            if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt - RefreshLeeway > DateTimeOffset.UtcNow)
                return _accessToken!;

            using var response = await _http.PostAsJsonAsync(RefreshPath, new
            {
                refreshToken = _refreshToken
            }, JsonOptions);

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(ExtractError(body) ?? $"HTTP {(int)response.StatusCode}");

            var auth = ParseAuthEnvelope(body)
                       ?? throw new InvalidOperationException("Refresh token thất bại.");

            await PersistAsync(auth, _userName);
            return _accessToken!;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public Task<bool> CanAcquireTokenSilentlyAsync()
    {
        var has = !string.IsNullOrWhiteSpace(_refreshToken) || !string.IsNullOrWhiteSpace(_accessToken);
        return Task.FromResult(has);
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(KeyAccessToken);
            var refresh = await SecureStorage.GetAsync(KeyRefreshToken);
            var expiresStr = await SecureStorage.GetAsync(KeyExpiresAt);
            var user = await SecureStorage.GetAsync(KeyUserName);

            if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(refresh))
                return false;

            _accessToken = token;
            _refreshToken = refresh;
            _userName = user;
            _expiresAt = DateTimeOffset.TryParse(expiresStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var exp)
                ? exp
                : DateTimeOffset.MinValue;

            // If access token still valid, we're done. Otherwise try to refresh.
            if (_expiresAt - RefreshLeeway > DateTimeOffset.UtcNow) return true;
            if (string.IsNullOrWhiteSpace(_refreshToken)) return false;

            await GetAccessTokenAsync();
            return IsSignedIn;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiAbpAuthService] Restore failed: {ex.Message}");
            await SignOutAsync();
            return false;
        }
    }

    public Task SignOutAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _userName = null;
        _expiresAt = DateTimeOffset.MinValue;

        SecureStorage.Remove(KeyAccessToken);
        SecureStorage.Remove(KeyRefreshToken);
        SecureStorage.Remove(KeyExpiresAt);
        SecureStorage.Remove(KeyUserName);
        return Task.CompletedTask;
    }

    private async Task PersistAsync(AbpAuthResult auth, string? username)
    {
        _accessToken = auth.AccessToken;
        _refreshToken = auth.RefreshToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(auth.ExpireInSeconds > 0 ? auth.ExpireInSeconds : 3600);
        _userName = username;

        await SecureStorage.SetAsync(KeyAccessToken, _accessToken ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(_refreshToken))
            await SecureStorage.SetAsync(KeyRefreshToken, _refreshToken);
        await SecureStorage.SetAsync(KeyExpiresAt, _expiresAt.ToString("O", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(_userName))
            await SecureStorage.SetAsync(KeyUserName, _userName);
    }

    private static AbpAuthResult? ParseAuthEnvelope(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        using var doc = JsonDocument.Parse(body);
        var resultEl = doc.RootElement.TryGetProperty("result", out var r) ? r : doc.RootElement;
        return JsonSerializer.Deserialize<AbpAuthResult>(resultEl.GetRawText(), JsonOptions);
    }

    private static string? ExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch (JsonException)
        {
        }
        return body.Length > 200 ? body[..200] : body;
    }

    private sealed class AbpAuthResult
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpireInSeconds { get; set; }
        public long UserId { get; set; }
    }
}
