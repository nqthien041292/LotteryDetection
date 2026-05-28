using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

/// <summary>
///     Drives the cold-start splash. Holds the brand on screen for a minimum
///     duration (no flicker) while it resolves where to send the user, then
///     routes to login.
/// </summary>
public class SplashViewModel : BaseViewModel
{
    private const int MinDisplayMs = 1000;

    private readonly INavigationService _navigationService;
    private bool _started;

    public SplashViewModel()
        : this(NavigationService.Default)
    {
    }

    public SplashViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public async Task RunStartupAsync()
    {
        // Per-instance guard. The process-level dedupe (so the restore itself
        // isn't repeated) is SplashStartup.InitialRestoreDone, set below.
        if (_started) return;
        _started = true;

        var minDelay = Task.Delay(MinDisplayMs);

        await minDelay;
        SplashStartup.InitialRestoreDone = true;

        try
        {
            await ResolveDestinationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Splash] Navigation failed: {ex.Message}");
            await _navigationService.NavigateToLoginWithSocialAsync();
        }
    }

    private Task ResolveDestinationAsync()
    {
        // Product request: every cold start should land on the social login
        // screen. This intentionally skips onboarding and stored-session
        // restore; interactive sign-in still navigates to Dashboard normally.
        return _navigationService.NavigateToLoginWithSocialAsync();
    }
}
