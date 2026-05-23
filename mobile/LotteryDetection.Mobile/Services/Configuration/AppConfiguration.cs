using System.Reflection;
using System.Text.Json;

namespace LotteryDetection.Mobile.Services.Configuration;

/// <summary>
///     Loads configuration from embedded appsettings.json resources.
/// </summary>
public static class AppConfiguration
{
    private static JsonDocument? _config;

    public static string GetVoiceApiBaseUrl()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("VoiceApi", out var voiceApi) &&
            voiceApi.TryGetProperty("BaseUrl", out var baseUrl))
            return baseUrl.GetString() ?? GetPlatformFallbackUrl();
        return GetPlatformFallbackUrl();
    }

    /// <summary>
    ///     Base URL for the LotteryDetection.Web.Host API. Returns null when not configured
    ///     so callers can fall back to mocks.
    /// </summary>
    public static string? GetBackendApiBaseUrl()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("Api", out var api) &&
            api.TryGetProperty("BaseUrl", out var baseUrl))
        {
            var value = baseUrl.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    public static (string? ClientId, string? TenantId) GetMicrosoftClient()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("AzureAd", out var azureAd))
        {
            var clientId = azureAd.TryGetProperty("ClientId", out var c) ? c.GetString() : null;
            var tenantId = azureAd.TryGetProperty("TenantId", out var t) ? t.GetString() : null;
            if (!string.IsNullOrWhiteSpace(clientId))
                return (clientId, string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId);
        }
        
        if (_config!.RootElement.TryGetProperty("Microsoft", out var ms))
        {
            var clientId = ms.TryGetProperty("ClientId", out var c) ? c.GetString() : null;
            var tenantId = ms.TryGetProperty("TenantId", out var t) ? t.GetString() : null;
            return (string.IsNullOrWhiteSpace(clientId) ? null : clientId,
                    string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId);
        }
        return (null, "common");
    }

    public static string? GetGoogleClientId()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("Google", out var g) &&
            g.TryGetProperty("ClientId", out var c))
        {
            var value = c.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    public static string GetAzureAdClientId()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("AzureAd", out var azureAd) &&
            azureAd.TryGetProperty("ClientId", out var clientId))
            return clientId.GetString() ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
        throw new InvalidOperationException("AzureAd:ClientId not configured");
    }

    public static string GetAzureAdTenantId()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("AzureAd", out var azureAd) &&
            azureAd.TryGetProperty("TenantId", out var tenantId))
            return tenantId.GetString() ?? throw new InvalidOperationException("AzureAd:TenantId not configured");
        throw new InvalidOperationException("AzureAd:TenantId not configured");
    }

    public static string GetAzureAdBackendClientId()
    {
        EnsureLoaded();
        if (_config!.RootElement.TryGetProperty("AzureAd", out var azureAd) &&
            azureAd.TryGetProperty("BackendClientId", out var backendClientId))
            return backendClientId.GetString() ??
                   throw new InvalidOperationException("AzureAd:BackendClientId not configured");
        throw new InvalidOperationException("AzureAd:BackendClientId not configured");
    }

    private static void EnsureLoaded()
    {
        if (_config != null) return;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "LotteryDetection.Mobile.appsettings.json";

#if DEBUG
        var devResourceName = "LotteryDetection.Mobile.appsettings.Development.json";
        if (assembly.GetManifestResourceNames().Contains(devResourceName))
            resourceName = devResourceName;
#endif

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            _config = JsonDocument.Parse("{}");
            return;
        }

        _config = JsonDocument.Parse(stream);
    }

    private static string GetPlatformFallbackUrl()
    {
        // Targets LotteryDetection.Web.Host's default Kestrel binding (https://localhost:44301).
        // Android emulator can't reach the host machine via "localhost" — 10.0.2.2 maps to it instead.
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "https://10.0.2.2:44301/";
        return "https://localhost:44301/";
    }
}