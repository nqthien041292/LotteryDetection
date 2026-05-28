using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Authentication;

namespace LotteryDetection.Mobile.Services.Auth;

/// <summary>
/// Facebook OAuth Implicit Grant via /v18.0/dialog/oauth — simpler than
/// Google because Facebook still supports the token-response flow for
/// installed apps and exposes user info on Graph /me with just the
/// access_token. The matching <c>fb{AppId}://authorize</c> URL scheme must
/// be registered in iOS Info.plist (CFBundleURLSchemes).
/// </summary>
public sealed class FacebookAuthHelper
{
    private readonly string _appId;
    private readonly string _redirectUri;

    public FacebookAuthHelper(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            throw new InvalidOperationException("Facebook AppId chưa cấu hình.");
        _appId = appId;
        _redirectUri = $"fb{appId}://authorize";
    }

    public async Task<ExternalAuthResult> SignInAsync()
    {
        var authUrl = new Uri(
            "https://www.facebook.com/v18.0/dialog/oauth" +
            $"?client_id={Uri.EscapeDataString(_appId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
            "&response_type=token" +
            "&scope=email,public_profile");

        var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, new Uri(_redirectUri));

        // WebAuthenticator parses access_token from the fragment/query into
        // both AccessToken and Properties — prefer the strongly typed slot.
        var accessToken = result.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken) &&
            result.Properties.TryGetValue("access_token", out var tok))
        {
            accessToken = tok;
        }
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Facebook sign-in không trả về access_token.");

        using var http = new HttpClient();
        var meJson = await http.GetStringAsync(
            $"https://graph.facebook.com/me?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}");
        using var doc = JsonDocument.Parse(meJson);
        var providerKey = doc.RootElement.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
            ? idEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new InvalidOperationException("Facebook không trả về user id (Graph /me thiếu trường id).");

        var displayName = doc.RootElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString()
            : null;

        return new ExternalAuthResult
        {
            AccessToken = accessToken,
            ProviderKey = providerKey,
            DisplayName = displayName
        };
    }
}
