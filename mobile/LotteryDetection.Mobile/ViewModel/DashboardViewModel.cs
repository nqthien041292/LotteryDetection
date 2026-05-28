using System.Windows.Input;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class DashboardViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;
    private readonly IAuthService? authService;

    public DashboardViewModel()
        : this(NavigationService.Default, ResolveAuthService())
    {
    }

    public DashboardViewModel(INavigationService navigationService, IAuthService? authService)
    {
        this.navigationService = navigationService;
        this.authService = authService;

        OpenLotteryCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
        OpenLotteryHistoryCommand = new Command(async () => await navigationService.NavigateToLotteryHistoryAsync());
        OpenLotteryResultsCommand = new Command(async () => await navigationService.NavigateToLotteryResultsAsync());
        OpenSettingsCommand = new Command(async () => await navigationService.NavigateToSettingsAsync());
    }

    public ICommand OpenLotteryCaptureCommand { get; }
    public ICommand OpenLotteryHistoryCommand { get; }
    public ICommand OpenLotteryResultsCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    // Read live from IAuthService each time so the silent MSAL refresh kicked
    // off by the splash (which may finish *after* the page was first bound) is
    // reflected without rebuilding the view-model.
    public string UserDisplayName =>
        !string.IsNullOrWhiteSpace(authService?.UserDisplayName)
            ? authService!.UserDisplayName!
            : "Khách";

    public string? AvatarImagePath
    {
        get
        {
            var path = Preferences.Get(SettingsViewModel.PrefAvatarPathKey, null as string);
            return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path) ? path : null;
        }
    }

    public bool HasCustomAvatar => !string.IsNullOrEmpty(AvatarImagePath);
    public bool HasNoCustomAvatar => !HasCustomAvatar;
    public ImageSource? AvatarSource =>
        HasCustomAvatar ? ImageSource.FromFile(AvatarImagePath!) : null;

    public void RefreshUserDisplayName()
    {
        NotifyPropertyChanged(nameof(UserDisplayName));
        NotifyPropertyChanged(nameof(AvatarImagePath));
        NotifyPropertyChanged(nameof(AvatarSource));
        NotifyPropertyChanged(nameof(HasCustomAvatar));
        NotifyPropertyChanged(nameof(HasNoCustomAvatar));
    }

    private static IAuthService? ResolveAuthService()
        => IPlatformApplication.Current?.Services.GetService<IAuthService>();
}
