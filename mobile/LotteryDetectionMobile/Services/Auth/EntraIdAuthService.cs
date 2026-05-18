using Microsoft.Identity.Client;

namespace LotteryDetectionMobile.Services.Auth;

/// <summary>
///     MSAL-based Entra ID authentication service.
///     Implements OAuth2 Authorization Code flow with PKCE.
///     Token cache is managed natively by MSAL (iOS Keychain / Android AccountManager).
/// </summary>
public class EntraIdAuthService : IAuthService
{
    private const string SessionUserEmailKey = "auth_user_email";
    private const string SessionUserDisplayNameKey = "auth_user_display_name";

    private readonly IPublicClientApplication _msal;
    private IAccount? _cachedAccount;
    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiresOn = DateTimeOffset.MinValue;

    // Fallback display values loaded from SecureStorage before MSAL restores
    private string? _fallbackDisplayName;
    private string? _fallbackEmail;

    public bool IsSignedIn => _cachedAccount != null || !string.IsNullOrEmpty(_cachedAccessToken);

    public string? UserDisplayName => !string.IsNullOrWhiteSpace(_fallbackDisplayName)
        ? _fallbackDisplayName
        : _cachedAccount?.Username?.Split('@').FirstOrDefault();

    public string? UserEmail => _cachedAccount?.Username ?? _fallbackEmail;

    public EntraIdAuthService()
    {
        var builder = PublicClientApplicationBuilder
            .Create(AuthConstants.ClientId)
            .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
            .WithRedirectUri(AuthConstants.RedirectUri);

#if IOS
        // MSAL persists its token cache in a keychain access group of the form
        // "<seed-prefix>.<group>". That fully-expanded group MUST be listed in the
        // active Entitlements*.plist or the keychain write fails. Device builds resolve
        // <seed-prefix> from the provisioning profile (the $(AppIdentifierPrefix) macro
        // expands to the TeamID); the simulator is ad-hoc signed with no profile, so the
        // macro never expands and <seed-prefix> resolves to the bundle id's first
        // segment ("com"). The matching literal groups are declared per-environment in
        // Entitlements.plist (device) / Entitlements.Simulator.plist (simulator), so the
        // group is set unconditionally to keep MSAL's lookup deterministic.
        builder = builder.WithIosKeychainSecurityGroup(AuthConstants.IosKeychainGroup);
#endif

        // MSAL handles token cache natively on mobile (iOS Keychain / Android SharedPrefs).
        // Custom SetBeforeAccess/SetAfterAccess throws PlatformNotSupportedException on mobile.
        _msal = builder.Build();
    }

#if IOS
    private static bool IsRunningOnSimulator()
    {
        return ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR;
    }
#endif

    private static bool IsKeychainError(Exception ex)
    {
        return ex is MsalClientException msalEx &&
               (msalEx.ErrorCode == "missing_keychain_access_group" ||
                msalEx.Message.Contains("keychain", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> SignInAsync()
    {
        AuthenticationResult result;

        try
        {
            var accounts = await _msal.GetAccountsAsync();
            _cachedAccount = accounts.FirstOrDefault();

            if (_cachedAccount != null)
                // Try silent acquisition first (uses persisted refresh token)
                result = await _msal.AcquireTokenSilent(AuthConstants.Scopes, _cachedAccount)
                    .ExecuteAsync();
            else
                // No cached account, go to interactive
                result = await AcquireTokenInteractiveAsync();
        }
        catch (MsalUiRequiredException)
        {
            result = await AcquireTokenInteractiveAsync();
        }
        catch (Exception ex) when (IsKeychainError(ex))
        {
            result = await AcquireTokenInteractiveAsync();
        }

        CacheResult(result);
        await SaveUserInfoAsync();
        return result.AccessToken;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // Check in-memory token first (5-minute buffer before expiry)
        if (!string.IsNullOrEmpty(_cachedAccessToken) && _tokenExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            return _cachedAccessToken;

        try
        {
            if (_cachedAccount == null)
            {
                var accounts = await _msal.GetAccountsAsync();
                _cachedAccount = accounts.FirstOrDefault();
            }

            if (_cachedAccount == null)
                throw new InvalidOperationException("User not signed in. Call SignInAsync first.");

            var result = await _msal.AcquireTokenSilent(AuthConstants.Scopes, _cachedAccount)
                .ExecuteAsync();
            CacheResult(result);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            var result = await AcquireTokenInteractiveAsync();
            CacheResult(result);
            return result.AccessToken;
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            var accounts = await _msal.GetAccountsAsync();
            foreach (var account in accounts) await _msal.RemoveAsync(account);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] Error during MSAL sign-out: {ex.Message}");
        }

        _cachedAccount = null;
        _cachedAccessToken = null;
        _tokenExpiresOn = DateTimeOffset.MinValue;
        _fallbackDisplayName = null;
        _fallbackEmail = null;

        // Clear persisted user display info
        SecureStorage.Remove(SessionUserEmailKey);
        SecureStorage.Remove(SessionUserDisplayNameKey);
    }

    public async Task<bool> CanAcquireTokenSilentlyAsync()
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && _tokenExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            return true;

        try
        {
            var accounts = await _msal.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            if (account == null) return false;

            await _msal.AcquireTokenSilent(AuthConstants.Scopes, account).ExecuteAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        // Check in-memory token first
        if (!string.IsNullOrEmpty(_cachedAccessToken) && _tokenExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            return true;

        // Load fallback user display info from SecureStorage
        await LoadUserInfoAsync();

        try
        {
            var accounts = await _msal.GetAccountsAsync();
            _cachedAccount = accounts.FirstOrDefault();

            if (_cachedAccount == null)
            {
                Console.WriteLine("[Auth] No cached accounts found. Session restore failed.");
                return false;
            }

            // Silent acquisition using persisted refresh token
            var result = await _msal.AcquireTokenSilent(AuthConstants.Scopes, _cachedAccount)
                .ExecuteAsync();
            CacheResult(result);
            Console.WriteLine($"[Auth] Session restored for {_cachedAccount.Username}");
            return true;
        }
        catch (MsalUiRequiredException)
        {
            Console.WriteLine("[Auth] Session expired, interactive login required.");
            _cachedAccount = null;
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] Session restore error: {ex.Message}");
            _cachedAccount = null;
            return false;
        }
    }

    private void CacheResult(AuthenticationResult result)
    {
        _cachedAccount = result.Account;
        _cachedAccessToken = result.AccessToken;
        _tokenExpiresOn = result.ExpiresOn;
    }

    /// <summary>
    ///     Saves user display info to SecureStorage (for showing name while restoring session).
    /// </summary>
    private async Task SaveUserInfoAsync()
    {
        try
        {
            var email = _cachedAccount?.Username;
            if (string.IsNullOrEmpty(email)) return;

            await SecureStorage.SetAsync(SessionUserEmailKey, email);
            await SecureStorage.SetAsync(SessionUserDisplayNameKey,
                email.Split('@').FirstOrDefault() ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] Failed to save user info: {ex.Message}");
        }
    }

    private async Task LoadUserInfoAsync()
    {
        try
        {
            _fallbackEmail = await SecureStorage.GetAsync(SessionUserEmailKey);
            _fallbackDisplayName = await SecureStorage.GetAsync(SessionUserDisplayNameKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] Failed to load user info: {ex.Message}");
        }
    }

    private async Task<AuthenticationResult> AcquireTokenInteractiveAsync()
    {
#if IOS
        var parentWindow = Platform.GetCurrentUIViewController();
        var acquireBuilder = _msal.AcquireTokenInteractive(AuthConstants.Scopes)
            .WithParentActivityOrWindow(parentWindow);

        if (IsRunningOnSimulator())
        {
            acquireBuilder = acquireBuilder.WithUseEmbeddedWebView(false);
        }

        return await acquireBuilder.ExecuteAsync();
#elif ANDROID
        var activity = Platform.CurrentActivity;
        return await _msal.AcquireTokenInteractive(AuthConstants.Scopes)
            .WithParentActivityOrWindow(activity)
            .ExecuteAsync();
#else
        return await _msal.AcquireTokenInteractive(AuthConstants.Scopes)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync();
#endif
    }
}
