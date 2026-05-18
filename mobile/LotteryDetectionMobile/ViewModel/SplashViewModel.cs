using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

/// <summary>
///     Drives the cold-start splash. Holds the brand on screen for a minimum
///     duration (no flicker) while it resolves where to send the user, then
///     routes to onboarding, dashboard, or login.
/// </summary>
public class SplashViewModel : BaseViewModel
{
    private const int MinDisplayMs = 1000;
    private const int RestoreTimeoutMs = 8000;

    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private bool _started;

    public SplashViewModel()
        : this(NavigationService.Default, GetAuthService())
    {
    }

    public SplashViewModel(INavigationService navigationService, IAuthService authService)
    {
        _navigationService = navigationService;
        _authService = authService;
    }

    public async Task RunStartupAsync()
    {
        // Per-instance guard. The process-level dedupe (so the restore itself
        // isn't repeated) is SplashStartup.InitialRestoreDone, set below.
        if (_started) return;
        _started = true;

        var minDelay = Task.Delay(MinDisplayMs);

        var destination = await ResolveDestinationAsync();

        await minDelay;
        SplashStartup.InitialRestoreDone = true;

        try
        {
            switch (destination)
            {
                case StartupDestination.Onboarding:
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Shell.Current.GoToAsync("//onboarding"));
                    break;
                case StartupDestination.Dashboard:
                    await _navigationService.NavigateToDashboardAsync();
                    break;
                default:
                    await _navigationService.NavigateToLoginWithSocialAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Splash] Navigation failed: {ex.Message}");
            await _navigationService.NavigateToLoginWithSocialAsync();
        }
    }

    private async Task<StartupDestination> ResolveDestinationAsync()
    {
        try
        {
            if (!Preferences.Get("OnboardingCompleted", false))
                return StartupDestination.Onboarding;

            if (_authService.IsSignedIn)
                return StartupDestination.Dashboard;

            // Silent token refresh hits the network with no internal timeout;
            // cap it so a dead/slow connection can't trap the user on the splash.
            var restoreTask = _authService.TryRestoreSessionAsync();
            var finished = await Task.WhenAny(restoreTask, Task.Delay(RestoreTimeoutMs));
            if (finished != restoreTask)
            {
                Console.WriteLine("[Splash] Session restore timed out; routing to login.");
                return StartupDestination.Login;
            }

            return await restoreTask ? StartupDestination.Dashboard : StartupDestination.Login;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Splash] Startup resolve failed: {ex.Message}");
            return StartupDestination.Login;
        }
    }

    private enum StartupDestination
    {
        Onboarding,
        Login,
        Dashboard
    }

    private static IAuthService GetAuthService()
    {
        var services = IPlatformApplication.Current?.Services;
        return services?.GetService<IAuthService>() ?? new EntraIdAuthService();
    }
}
