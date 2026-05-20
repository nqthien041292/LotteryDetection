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
            result = await _app.AcquireTokenInteractive(Scopes).ExecuteAsync();
        }

        return new MicrosoftAuthResult
        {
            AccessToken = result.AccessToken,
            IdToken = result.IdToken,
            Account = result.Account?.Username,
            // The `oid` claim — same Azure AD Object ID Graph /me returns,
            // which ABP uses as the provider key for external login matching.
            ProviderKey = result.UniqueId ?? result.Account?.HomeAccountId?.ObjectId ?? string.Empty
        };
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
    public string ProviderKey { get; init; } = string.Empty;
}
