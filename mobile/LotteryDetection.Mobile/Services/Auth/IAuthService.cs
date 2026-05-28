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
    ///     <paramref name="providerKey" /> is the user's stable id at the
    ///     provider (Microsoft <c>oid</c> / Google <c>sub</c>); ABP cross-checks
    ///     it against the provider's userinfo endpoint.
    /// </summary>
    Task<string> SignInExternalAsync(string provider, string providerKey, string providerAccessCode, string? displayName = null);

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

    /// <summary>
    ///     Overwrite the cached <see cref="UserDisplayName" /> (and persist).
    ///     Used after a silent MSAL refresh to recover the real user name when
    ///     the session was originally persisted with a placeholder like
    ///     <c>"Microsoft:external"</c>.
    /// </summary>
    Task SetDisplayNameAsync(string displayName);

    /// <summary>
    ///     POST the image bytes as the signed-in user's profile picture.
    ///     The backend stores it as an ABP BinaryObject and links it to
    ///     <c>User.ProfilePictureId</c>.
    /// </summary>
    Task UploadProfilePictureAsync(byte[] imageBytes, string contentType);
}