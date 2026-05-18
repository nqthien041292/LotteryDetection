using LotteryDetectionMobile.Services.Configuration;

namespace LotteryDetectionMobile.Services.Auth;

/// <summary>
///     Entra ID authentication constants.
///     Values loaded from appsettings.json (AzureAd section).
/// </summary>
public static class AuthConstants
{
    /// <summary>
    ///     iOS Keychain access group for token persistence.
    /// </summary>
    public const string IosKeychainGroup = "com.lotterydetection.mobile";

    /// <summary>
    ///     Mobile app's Application (client) ID from Azure Portal.
    /// </summary>
    public static string ClientId => AppConfiguration.GetAzureAdClientId();

    /// <summary>
    ///     Directory (tenant) ID from Azure Portal.
    /// </summary>
    public static string TenantId => AppConfiguration.GetAzureAdTenantId();

    /// <summary>
    ///     OAuth2 authority URL. Uses /common to support both personal and work accounts.
    /// </summary>
    public static string Authority => "https://login.microsoftonline.com/common";

    /// <summary>
    ///     API scopes to request. Must match "Expose an API" in backend registration.
    /// </summary>
    public static string[] Scopes => [$"api://{AppConfiguration.GetAzureAdBackendClientId()}/access"];

    /// <summary>
    ///     Redirect URI for MSAL. Format: msal{ClientId}://auth
    ///     Must match redirect URI in Azure Portal.
    /// </summary>
    public static string RedirectUri => $"msal{ClientId}://auth";
}