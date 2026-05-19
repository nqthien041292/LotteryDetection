using Foundation;

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
}
