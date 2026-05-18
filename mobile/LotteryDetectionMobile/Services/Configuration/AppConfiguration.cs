using System.Reflection;
using System.Text.Json;

namespace LotteryDetectionMobile.Services.Configuration;

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
        var resourceName = "LotteryDetectionMobile.appsettings.json";

#if DEBUG
        var devResourceName = "LotteryDetectionMobile.appsettings.Development.json";
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