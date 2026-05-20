namespace LotteryDetection.Mobile.Services.Auth;

/// <summary>
///     Authentication service interface for Entra ID OAuth2.
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     Whether user is currently signed in (has cached account).
    /// </summary>
    bool IsSignedIn { get; }

    /// <summary>
    ///     Current signed-in user's display name.
    /// </summary>
    string? UserDisplayName { get; }

    /// <summary>
    ///     Current signed-in user's email/UPN.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    ///     Sign in user. Uses silent auth first, falls back to interactive.
    /// </summary>
    /// <returns>Access token for API calls.</returns>
    Task<string> SignInAsync();

    /// <summary>
    ///     Exchange a third-party provider token (Microsoft / Google) for an
    ///     ABP-issued bearer token via /api/TokenAuth/ExternalAuthenticate.
    /// </summary>
    Task<string> SignInExternalAsync(string provider, string providerAccessCode);

    /// <summary>
    ///     Get access token. Uses silent auth (cached/refreshed token).
    ///     Throws if user not signed in.
    /// </summary>
    /// <returns>Access token for API calls.</returns>
    Task<string> GetAccessTokenAsync();

    /// <summary>
    ///     Sign out user. Clears all cached tokens.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    ///     Check if token can be acquired silently (user has valid session).
    /// </summary>
    Task<bool> CanAcquireTokenSilentlyAsync();

    /// <summary>
    ///     Try to restore session from cached credentials.
    ///     Returns true if session restored, false otherwise.
    /// </summary>
    Task<bool> TryRestoreSessionAsync();
}