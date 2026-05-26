using Foundation;
using Plugin.Firebase;
using Plugin.Firebase.Core.Platforms.iOS;

namespace LotteryDetection.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary launchOptions)
    {
        // CrossFirebase.Initialize(); // Commented out to prevent crash if GoogleService-Info.plist is missing
        return base.FinishedLaunching(application, launchOptions);
    }

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
}
