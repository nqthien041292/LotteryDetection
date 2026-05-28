using System.Text.Json;
using Microsoft.Identity.Client;

namespace LotteryDetection.Mobile.Services.Auth;

/// <summary>
///     Acquires a Microsoft Entra ID token via MSAL and hands the bearer
///     access_token to <see cref="ApiAbpAuthService" /> for ABP exchange.
/// </summary>
public sealed class MicrosoftAuthHelper
{
    // User.Read is required so the issued access_token can call Graph /me —
    // that's what ABP's MicrosoftAuthProviderApi hits server-side. OIDC scopes
    // give us the id_token + oid claim used as the provider key.
    private static readonly string[] Scopes = { "openid", "profile", "email", "User.Read" };

    private readonly IPublicClientApplication _app;

    public MicrosoftAuthHelper(string clientId, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Microsoft ClientId chưa cấu hình.");

        _app = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic,
                string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId)
            // msauth.<BundleId>://auth — must match the iOS platform entry
            // added in Entra app registration + the URL scheme in Info.plist.
            .WithRedirectUri($"msauth.com.lotterydetection.mobile://auth")
            .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
            .Build();
    }

    public async Task<MicrosoftAuthResult> SignInAsync()
    {
        // Try silent first if the user has signed in before this session.
        var accounts = await _app.GetAccountsAsync();
        AuthenticationResult? result = null;

        if (accounts.Any())
        {
            try
            {
                result = await _app.AcquireTokenSilent(Scopes, accounts.First()).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = null;
            }
        }

        if (result == null)
        {
            // MSAL 4.50+ auto-discovers the parent UIViewController on iOS
            // when running under MAUI, so we don't need WithParentActivityOrWindow.
#if ANDROID
            result = await _app.AcquireTokenInteractive(Scopes)
                .WithParentActivityOrWindow(Microsoft.Maui.ApplicationModel.Platform.CurrentActivity)
                .ExecuteAsync();
#else
            result = await _app.AcquireTokenInteractive(Scopes).ExecuteAsync();
#endif
        }

        var displayName = ExtractDisplayNameFromIdToken(result.IdToken)
                          ?? result.Account?.Username;

        return new MicrosoftAuthResult
        {
            AccessToken = result.AccessToken,
            IdToken = result.IdToken,
            Account = result.Account?.Username,
            DisplayName = displayName,
            // The `oid` claim — same Azure AD Object ID Graph /me returns,
            // which ABP uses as the provider key for external login matching.
            ProviderKey = result.UniqueId ?? result.Account?.HomeAccountId?.ObjectId ?? string.Empty
        };
    }

    /// <summary>
    /// Pulls the user's full name out of the MSAL ID token's `name` claim.
    /// Falls back to `preferred_username` (typically the email/UPN) if `name`
    /// isn't present.
    /// </summary>
    private static string? ExtractDisplayNameFromIdToken(string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken)) return null;
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                return nameEl.GetString();
            if (doc.RootElement.TryGetProperty("preferred_username", out var upnEl) && upnEl.ValueKind == JsonValueKind.String)
                return upnEl.GetString();
        }
        catch
        {
            // best-effort — caller falls back to Account.Username
        }
        return null;
    }

    /// <summary>
    /// Silent-only token acquisition for cases where we want to refresh the
    /// id-token claims (e.g. display name) without ever prompting the user.
    /// Returns null when MSAL has no cached account, when silent acquisition
    /// would require UI, or on any other failure.
    /// </summary>
    public async Task<MicrosoftAuthResult?> TryGetSilentResultAsync()
    {
        try
        {
            var accounts = await _app.GetAccountsAsync();
            var first = accounts.FirstOrDefault();
            if (first == null) return null;

            var result = await _app.AcquireTokenSilent(Scopes, first).ExecuteAsync();
            return new MicrosoftAuthResult
            {
                AccessToken = result.AccessToken,
                IdToken = result.IdToken,
                Account = result.Account?.Username,
                DisplayName = ExtractDisplayNameFromIdToken(result.IdToken) ?? result.Account?.Username,
                ProviderKey = result.UniqueId ?? result.Account?.HomeAccountId?.ObjectId ?? string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        foreach (var account in await _app.GetAccountsAsync())
            await _app.RemoveAsync(account);
    }
}

public sealed class MicrosoftAuthResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string? IdToken { get; init; }
    public string? Account { get; init; }
    public string? DisplayName { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
}
