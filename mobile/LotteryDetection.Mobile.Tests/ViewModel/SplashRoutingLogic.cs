namespace LotteryDetection.Mobile.Tests.ViewModel;

/// <summary>
///     Extracted splash routing decision for testing.
///     This mirrors SplashViewModel.ResolveDestinationAsync logic.
///     The app project multi-targets platform TFMs and cannot be referenced
///     from this net9.0 test assembly, so the pure decision is duplicated here
///     (same convention as ChunkSenderLogic mirroring HybridVoiceService).
/// </summary>
public enum StartupDestination
{
    Onboarding,
    Login,
    Dashboard
}

public class SplashRoutingLogic
{
    public const int DefaultRestoreTimeoutMs = 8000;

    /// <summary>
    ///     Resolves where a cold-started app should navigate.
    /// </summary>
    /// <param name="onboardingCompleted">Value of Preferences "OnboardingCompleted".</param>
    /// <param name="isSignedIn">IAuthService.IsSignedIn.</param>
    /// <param name="tryRestoreSession">IAuthService.TryRestoreSessionAsync.</param>
    /// <param name="restoreTimeoutMs">Upper bound on the silent restore.</param>
    public static async Task<StartupDestination> ResolveDestinationAsync(
        bool onboardingCompleted,
        bool isSignedIn,
        Func<Task<bool>> tryRestoreSession,
        int restoreTimeoutMs = DefaultRestoreTimeoutMs)
    {
        try
        {
            if (!onboardingCompleted)
                return StartupDestination.Onboarding;

            if (isSignedIn)
                return StartupDestination.Dashboard;

            var restoreTask = tryRestoreSession();
            var finished = await Task.WhenAny(restoreTask, Task.Delay(restoreTimeoutMs));
            if (finished != restoreTask)
                return StartupDestination.Login;

            return await restoreTask ? StartupDestination.Dashboard : StartupDestination.Login;
        }
        catch
        {
            return StartupDestination.Login;
        }
    }
}
