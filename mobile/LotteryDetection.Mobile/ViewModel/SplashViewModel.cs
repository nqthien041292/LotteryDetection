using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

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
            return Preferences.Get("OnboardingCompleted", false)
                ? StartupDestination.Dashboard
                : StartupDestination.Onboarding;
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
        return services?.GetService<IAuthService>() ?? MockAuthService.Instance;
    }
}
