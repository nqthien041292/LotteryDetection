using System.Windows.Input;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Configuration;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.ViewModel;

/// <summary>
///     ViewModel for the login page. Uses Microsoft Entra ID for authentication.
/// </summary>
public class LoginPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly SemaphoreSlim _sessionCheckLock = new(1, 1);
    private bool _initialSessionChecked;
    private string? _errorMessage;
    private bool _isBusy;
    private string? _statusMessage;

    public LoginPageViewModel()
        : this(NavigationService.Default, GetAuthService(), GetPushService())
    {
    }

    public LoginPageViewModel(INavigationService navigationService, IAuthService authService, IPushNotificationService pushNotificationService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _pushNotificationService = pushNotificationService;
        SignInWithMicrosoftCommand = new Command(async () => await SignInWithMicrosoftAsync(), () => IsNotBusy);
        SignInWithGoogleCommand = new Command(async () => await SignInWithGoogleAsync(), () => IsNotBusy);
        SignInWithFacebookCommand = new Command(async () => await SignInWithFacebookAsync(), () => IsNotBusy);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                NotifyPropertyChanged(nameof(HasError));
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
                NotifyPropertyChanged(nameof(HasStatus));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public new bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyPropertyChanged(nameof(IsNotBusy));
                (SignInWithMicrosoftCommand as Command)?.ChangeCanExecute();
                (SignInWithGoogleCommand as Command)?.ChangeCanExecute();
                (SignInWithFacebookCommand as Command)?.ChangeCanExecute();
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public ICommand SignInWithMicrosoftCommand { get; }
    public ICommand SignInWithGoogleCommand { get; }
    public ICommand SignInWithFacebookCommand { get; }

    /// <summary>
    ///     Called every time the LoginPage appears. Handles initial session restore
    ///     and also detects when the user has just returned from interactive browser
    ///     auth (e.g., on Android first-time OAuth where the SignInAsync continuation
    ///     may complete on a background thread before navigation can take effect).
    /// </summary>
    public async Task OnPageAppearingAsync()
    {
        if (!await _sessionCheckLock.WaitAsync(0)) return;
        try
        {
            // If interactive auth already completed (e.g., user returning from browser),
            // the auth service holds a valid token. Navigate straight to Dashboard.
            if (_authService.IsSignedIn)
            {
                await _pushNotificationService.InitializeAsync();
                _ = _pushNotificationService.RegisterTokenAsync();
                
                await _navigationService.NavigateToDashboardAsync();
                return;
            }

            // Splash already performed the cold-start restore; skip the
            // duplicate (the IsSignedIn check above still handles the
            // return-from-interactive-auth case).
            if (SplashStartup.InitialRestoreDone) return;

            if (_initialSessionChecked) return;
            _initialSessionChecked = true;

            StatusMessage = "Checking for existing session...";
            var restored = await _authService.TryRestoreSessionAsync();

            if (restored)
            {
                await _pushNotificationService.InitializeAsync();
                _ = _pushNotificationService.RegisterTokenAsync();
                
                StatusMessage = $"Welcome back, {_authService.UserDisplayName}!";
                await Task.Delay(500);
                await _navigationService.NavigateToDashboardAsync();
            }
            else
            {
                StatusMessage = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginPage] Session check failed: {ex.Message}");
            StatusMessage = null;
        }
        finally
        {
            _sessionCheckLock.Release();
        }
    }

    /// <summary>
    ///     Backward-compatible entry point. Forwards to OnPageAppearingAsync.
    /// </summary>
    public Task InitializeAsync() => OnPageAppearingAsync();

    private async Task SignInWithGoogleAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsBusy = true;
        try
        {
            StatusMessage = "Đang mở Google sign-in…";
            var clientId = AppConfiguration.GetGoogleClientId();
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("Google ClientId chưa cấu hình trong appsettings.json.");

            var helper = new GoogleAuthHelper(clientId);
            var result = await helper.SignInAsync();
            await _authService.SignInExternalAsync("Google", result.ProviderKey, result.AccessToken, result.DisplayName);

            StatusMessage = $"Welcome, {_authService.UserDisplayName}!";
            await Task.Delay(500);

            await _pushNotificationService.InitializeAsync();
            _ = _pushNotificationService.RegisterTokenAsync();

            await _navigationService.NavigateToDashboardAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginPage] Google sign-in failed: {ex}");
            if (IsUserCancellation(ex)) { StatusMessage = null; ErrorMessage = null; }
            else ErrorMessage = $"Google sign-in thất bại: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SignInWithFacebookAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsBusy = true;
        try
        {
            StatusMessage = "Đang mở Facebook sign-in…";
            var appId = AppConfiguration.GetFacebookAppId();
            if (string.IsNullOrWhiteSpace(appId))
                throw new InvalidOperationException("Facebook AppId chưa cấu hình trong appsettings.json.");

            var helper = new FacebookAuthHelper(appId);
            var result = await helper.SignInAsync();
            await _authService.SignInExternalAsync("Facebook", result.ProviderKey, result.AccessToken, result.DisplayName);

            StatusMessage = $"Welcome, {_authService.UserDisplayName}!";
            await Task.Delay(500);

            await _pushNotificationService.InitializeAsync();
            _ = _pushNotificationService.RegisterTokenAsync();

            await _navigationService.NavigateToDashboardAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginPage] Facebook sign-in failed: {ex}");
            if (IsUserCancellation(ex)) { StatusMessage = null; ErrorMessage = null; }
            else ErrorMessage = $"Facebook sign-in thất bại: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsUserCancellation(Exception ex)
        => ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("user_cancel", StringComparison.OrdinalIgnoreCase);

    private async Task SignInWithMicrosoftAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsBusy = true;

        try
        {
            StatusMessage = "Đang mở Microsoft sign-in…";

            var (clientId, tenantId) = AppConfiguration.GetMicrosoftClient();
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                // MSAL → access_token → ABP /api/TokenAuth/ExternalAuthenticate.
                var msal = new MicrosoftAuthHelper(clientId, tenantId ?? "common");
                var result = await msal.SignInAsync();
                await _authService.SignInExternalAsync("Microsoft", result.ProviderKey, result.AccessToken, result.DisplayName);
            }
            else
            {
                // No Microsoft config (e.g. local dev without backend) — fall back
                // to whatever SignInAsync does (prompt for credentials / mock).
                await _authService.SignInAsync();
            }

            StatusMessage = $"Welcome, {_authService.UserDisplayName}!";
            await Task.Delay(500);

            await _pushNotificationService.InitializeAsync();
            _ = _pushNotificationService.RegisterTokenAsync();

            await _navigationService.NavigateToLotteryCaptureAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginPage] Sign-in failed: {ex}");

            // Handle user cancellation gracefully
            if (ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("user_cancel", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = null;
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = $"Sign-in failed: {ex.Message}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    ///     Gets the auth service from DI or creates a default instance.
    /// </summary>
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