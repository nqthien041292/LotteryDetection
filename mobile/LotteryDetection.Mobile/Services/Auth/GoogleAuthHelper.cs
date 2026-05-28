using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Authentication;

namespace LotteryDetection.Mobile.Services.Auth;

/// <summary>
/// OAuth 2.0 Authorization Code + PKCE flow against Google's
/// /o/oauth2/v2/auth endpoint, using WebAuthenticator (ASWebAuthenticationSession
/// on iOS, Chrome Custom Tabs on Android). Google deprecated the implicit
/// "token" flow for installed apps, so we always exchange an authorization
/// code for an access_token + id_token.
/// </summary>
public sealed class GoogleAuthHelper
{
    private readonly string _clientId;
    private readonly string _redirectUri;

    public GoogleAuthHelper(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Google ClientId chưa cấu hình.");
        _clientId = clientId;

        // Google's recommended installed-app callback is the reversed client
        // ID — e.g. "com.googleusercontent.apps.123-abc". The matching URL
        // scheme must be registered in iOS Info.plist (CFBundleURLSchemes).
        var idPart = clientId.Replace(".apps.googleusercontent.com", "", StringComparison.OrdinalIgnoreCase);
        _redirectUri = $"com.googleusercontent.apps.{idPart}:/oauth2redirect";
    }

    public async Task<ExternalAuthResult> SignInAsync()
    {
        var verifier = GenerateCodeVerifier();
        var challenge = ToBase64UrlSha256(verifier);

        var authUrl = new Uri(
            "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(_clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid email profile")}" +
            $"&code_challenge={challenge}" +
            "&code_challenge_method=S256");

        var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, new Uri(_redirectUri));
        if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Google sign-in không trả về authorization code.");

        using var http = new HttpClient();
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _clientId,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier
        };
        using var tokenResp = await http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(form));
        var tokenJson = await tokenResp.Content.ReadAsStringAsync();
        if (!tokenResp.IsSuccessStatusCode)
            throw new HttpRequestException($"Google token exchange failed: {tokenJson}");

        using var doc = JsonDocument.Parse(tokenJson);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()
                          ?? throw new InvalidOperationException("Google access_token thiếu trong response.");
        var idToken = doc.RootElement.TryGetProperty("id_token", out var idTokenEl)
            ? idTokenEl.GetString() : null;

        return new ExternalAuthResult
        {
            AccessToken = accessToken,
            IdToken = idToken,
            ProviderKey = ExtractClaim(idToken, "sub") ?? string.Empty,
            DisplayName = ExtractClaim(idToken, "name")
        };
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url(bytes);
    }

    private static string ToBase64UrlSha256(string verifier)
        => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string? ExtractClaim(string? idToken, string claim)
    {
        if (string.IsNullOrWhiteSpace(idToken)) return null;
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            return doc.RootElement.TryGetProperty(claim, out var el) && el.ValueKind == JsonValueKind.String
                ? el.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
