namespace LotteryDetection.Mobile.ViewModel;

/// <summary>
///     Process-scoped flag set once the splash screen has performed the cold-start
///     session restore. Lets other pages skip a duplicate restore on first appear
///     while still allowing the return-from-interactive-auth navigation path.
/// </summary>
public static class SplashStartup
{
    public static bool InitialRestoreDone { get; set; }
}
