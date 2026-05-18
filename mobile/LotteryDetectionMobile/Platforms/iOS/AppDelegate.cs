using Foundation;
using Microsoft.Identity.Client;
using UIKit;
using LotteryDetectionMobile;

namespace LotteryDetectionMobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    // Console.Error lands in stderr which simctl captures via os_log. If launch ever
    // silently dies again, grep simlog.txt for "[FATAL] CreateMauiApp threw".
    protected override MauiApp CreateMauiApp()
    {
        try
        {
            return MauiProgram.CreateMauiApp();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FATAL] CreateMauiApp threw: {ex}");
            throw;
        }
    }

    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        // Broker response (via Microsoft Authenticator app)
        if (AuthenticationContinuationHelper.IsBrokerResponse(null))
        {
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url);
            return true;
        }

        // Standard web auth redirect (msal{clientId}://auth)
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url);
        return base.OpenUrl(application, url, options);
    }
}