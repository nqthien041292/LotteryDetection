using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Interfaces;
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
    private readonly IPushNotificationService _pushNotificationService;
    private bool _started;

    public SplashViewModel()
        : this(NavigationService.Default, GetAuthService(), GetPushService())
    {
    }

    public SplashViewModel(INavigationService navigationService, IAuthService authService, IPushNotificationService pushNotificationService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _pushNotificationService = pushNotificationService;
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
                case StartupDestination.LotteryCapture:
                    await _navigationService.NavigateToLotteryCaptureAsync();
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

            // Attempt to restore a previously persisted session. Cap the wait
            // so a hung token-refresh request can't keep the splash on screen.
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(RestoreTimeoutMs));
            var restoreTask = _authService.TryRestoreSessionAsync();
            var done = await Task.WhenAny(restoreTask, Task.Delay(Timeout.Infinite, cts.Token));

            if (done == restoreTask && await restoreTask && _authService.IsSignedIn)
            {
                // Initialize push notifications and register token
                await _pushNotificationService.InitializeAsync();
                _ = _pushNotificationService.RegisterTokenAsync(); // Fire and forget
                
                return StartupDestination.LotteryCapture;
            }

            return StartupDestination.Login;
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
        LotteryCapture
    }

    private static IAuthService GetAuthService()
    {
        var services = IPlatformApplication.Current?.Services;
        return services?.GetService<IAuthService>() ?? MockAuthService.Instance;
    }

    private static IPushNotificationService GetPushService()
    {
        var services = IPlatformApplication.Current?.Services;
        return services?.GetService<IPushNotificationService>() ?? new Services.PushNotificationService(MockLotteryDetectionService.Instance);
    }
}
