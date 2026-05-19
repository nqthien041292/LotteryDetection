using System.Windows.Input;
using LotteryDetectionMobile.Services;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class OnboardingViewModel : BaseViewModel
{
    private const int TotalSlides = 3;

    private readonly INavigationService navigationService;
    private readonly IPermissionService permissionService;

    private bool calendarEnabled;
    private bool microphoneEnabled;
    private int selectedIndex;
    private bool showPermissionPopup;

    public OnboardingViewModel()
        : this(PermissionService.Default, NavigationService.Default)
    {
    }

    public OnboardingViewModel(IPermissionService permissionService, INavigationService navigationService)
    {
        this.permissionService = permissionService;
        this.navigationService = navigationService;

        NextCommand = new Command(async () => await OnNextAsync());
        BackCommand = new Command(OnBack);
        SkipCommand = new Command(async () => await CompleteOnboardingAsync());
        AllowCommand = new Command(async () => await OnAllowPermissionsAsync());
        NotNowCommand = new Command(async () => await OnNotNowAsync());
    }

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand AllowCommand { get; }
    public ICommand NotNowCommand { get; }

    public bool ShowPermissionPopup
    {
        get => showPermissionPopup;
        set => SetProperty(ref showPermissionPopup, value);
    }

    public bool MicrophoneEnabled
    {
        get => microphoneEnabled;
        set => SetProperty(ref microphoneEnabled, value);
    }

    public bool CalendarEnabled
    {
        get => calendarEnabled;
        set => SetProperty(ref calendarEnabled, value);
    }

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            var clamped = Math.Clamp(value, 0, TotalSlides - 1);
            if (!SetProperty(ref selectedIndex, clamped)) return;

            NotifyPropertyChanged(nameof(IsSlide0));
            NotifyPropertyChanged(nameof(IsSlide1));
            NotifyPropertyChanged(nameof(IsSlide2));
            NotifyPropertyChanged(nameof(ShowSkipButton));
            NotifyPropertyChanged(nameof(ShowBackButton));
            NotifyPropertyChanged(nameof(NextButtonText));
            NotifyPropertyChanged(nameof(IsDot0Active));
            NotifyPropertyChanged(nameof(IsDot1Active));
            NotifyPropertyChanged(nameof(IsDot2Active));
        }
    }

    public bool IsSlide0 => SelectedIndex == 0;

    public bool IsSlide1 => SelectedIndex == 1;

    public bool IsSlide2 => SelectedIndex == 2;

    public bool IsDot0Active => SelectedIndex == 0;

    public bool IsDot1Active => SelectedIndex == 1;

    public bool IsDot2Active => SelectedIndex == 2;

    public bool ShowSkipButton => SelectedIndex < TotalSlides - 1;

    public bool ShowBackButton => SelectedIndex > 0;

    public string NextButtonText => SelectedIndex < TotalSlides - 1 ? "Tiếp tục" : "Bắt đầu dò vé";

    private async Task OnNextAsync()
    {
        if (SelectedIndex < TotalSlides - 1)
        {
            SelectedIndex++;
            return;
        }

        await CompleteOnboardingAsync();
    }

    private void OnBack()
    {
        if (SelectedIndex > 0) SelectedIndex--;
    }

    private async Task CompleteOnboardingAsync()
    {
        ShowPermissionPopup = true;
    }

    private async Task OnAllowPermissionsAsync()
    {
        ShowPermissionPopup = false;
        var cameraGranted = await Permissions.RequestAsync<Permissions.Camera>();
        MicrophoneEnabled = cameraGranted == PermissionStatus.Granted;
        CalendarEnabled = true;

        await FinishAndNavigateAsync();
    }

    private async Task OnNotNowAsync()
    {
        ShowPermissionPopup = false;
        await FinishAndNavigateAsync();
    }

    private async Task FinishAndNavigateAsync()
    {
        Preferences.Set("OnboardingCompleted", true);
        await navigationService.NavigateToDashboardAsync();
    }
}
