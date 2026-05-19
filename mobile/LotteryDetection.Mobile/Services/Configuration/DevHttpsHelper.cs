namespace LotteryDetection.Mobile.Services.Configuration;

/// <summary>
///     Builds HttpClient handlers that trust LotteryDetection.Web.Host's self-signed dev cert
///     when running against https://10.0.2.2:44301/ (Android) or https://localhost:44301/ (iOS).
///     Validation is bypassed only in DEBUG; RELEASE builds keep default certificate validation.
/// </summary>
public static class DevHttpsHelper
{
    public static HttpClientHandler CreateHandler(bool allowAutoRedirect = false)
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = allowAutoRedirect };
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif
        return handler;
    }
}
