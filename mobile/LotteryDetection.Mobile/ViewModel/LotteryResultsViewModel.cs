using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class LotteryResultsViewModel : BaseViewModel
{
    private readonly ILotteryResultsService resultsService;
    private readonly INavigationService navigationService;
    private string updatedAtLabel = string.Empty;
    private string heroDateLabel = string.Empty;
    private bool hasLoaded;
    private DateTime selectedDate = DateTime.Today;

    private IReadOnlyList<LotteryRegionDraw> allDraws = new List<LotteryRegionDraw>();
    private bool showBac = true;
    private bool showTrung = true;
    private bool showNam = true;

    public LotteryResultsViewModel(ILotteryResultsService resultsService, INavigationService navigationService)
    {
        this.resultsService = resultsService;
        this.navigationService = navigationService;
        Regions = new ObservableCollection<LotteryRegionDraw>();
        OpenCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
        OpenLiveResultsCommand = new Command(async () => await navigationService.NavigateToLotteryLiveResultsAsync());

        // Load local preferences; if a previous build persisted all-three-off
        // (no longer reachable via UI), recover by enabling all regions.
        showBac = Microsoft.Maui.Storage.Preferences.Get("ShowRegionBac", true);
        showTrung = Microsoft.Maui.Storage.Preferences.Get("ShowRegionTrung", true);
        showNam = Microsoft.Maui.Storage.Preferences.Get("ShowRegionNam", true);
        if (!showBac && !showTrung && !showNam)
        {
            showBac = showTrung = showNam = true;
        }

        ToggleBacCommand = new Command(ToggleBac);
        ToggleTrungCommand = new Command(ToggleTrung);
        ToggleNamCommand = new Command(ToggleNam);
    }

    public ObservableCollection<LotteryRegionDraw> Regions { get; }
    public ICommand OpenCaptureCommand { get; }
    public ICommand OpenLiveResultsCommand { get; }
    public ICommand ToggleBacCommand { get; }
    public ICommand ToggleTrungCommand { get; }
    public ICommand ToggleNamCommand { get; }

    public bool HasRegions => Regions.Count > 0;
    public bool ShowSkeleton => IsBusy && !hasLoaded;
    public bool ShowEmptyState => !HasRegions && !IsBusy;

    public bool IsAnyActiveRegionLive
    {
        get
        {
            var now = DateTime.Now;
            var time = now.TimeOfDay;

            // South (Nam): 16:10 to 16:45
            bool isNamLive = time >= new TimeSpan(16, 10, 0) && time <= new TimeSpan(16, 45, 0);
            // Central (Trung): 17:10 to 17:45
            bool isTrungLive = time >= new TimeSpan(17, 10, 0) && time <= new TimeSpan(17, 45, 0);
            // North (Bac): 18:10 to 18:45
            bool isBacLive = time >= new TimeSpan(18, 10, 0) && time <= new TimeSpan(18, 45, 0);

            return (ShowNam && isNamLive) || (ShowTrung && isTrungLive) || (ShowBac && isBacLive);
        }
    }

    public bool ShowBac
    {
        get => showBac;
        set
        {
            if (SetProperty(ref showBac, value))
            {
                Microsoft.Maui.Storage.Preferences.Set("ShowRegionBac", value);
                NotifyChipStyles();
            }
        }
    }

    public bool ShowTrung
    {
        get => showTrung;
        set
        {
            if (SetProperty(ref showTrung, value))
            {
                Microsoft.Maui.Storage.Preferences.Set("ShowRegionTrung", value);
                NotifyChipStyles();
            }
        }
    }

    public bool ShowNam
    {
        get => showNam;
        set
        {
            if (SetProperty(ref showNam, value))
            {
                Microsoft.Maui.Storage.Preferences.Set("ShowRegionNam", value);
                NotifyChipStyles();
            }
        }
    }

    // Chip Visual Style Helpers
    public string BacChipBg => ShowBac ? "#16A34A" : "#F1F5F9";
    public string BacChipText => ShowBac ? "#FFFFFF" : "#94A3B8";
    public string BacChipStroke => ShowBac ? "#16A34A" : "#E2E8F0";

    public string TrungChipBg => ShowTrung ? "#F97316" : "#F1F5F9";
    public string TrungChipText => ShowTrung ? "#FFFFFF" : "#94A3B8";
    public string TrungChipStroke => ShowTrung ? "#F97316" : "#E2E8F0";

    public string NamChipBg => ShowNam ? "#2563EB" : "#F1F5F9";
    public string NamChipText => ShowNam ? "#FFFFFF" : "#94A3B8";
    public string NamChipStroke => ShowNam ? "#2563EB" : "#E2E8F0";

    public string UpdatedAtLabel
    {
        get => updatedAtLabel;
        private set => SetProperty(ref updatedAtLabel, value);
    }

    public string HeroDateLabel
    {
        get => heroDateLabel;
        private set => SetProperty(ref heroDateLabel, value);
    }

    public DateTime SelectedDate
    {
        get => selectedDate;
        set
        {
            if (SetProperty(ref selectedDate, value))
            {
                _ = LoadDataForSelectedDateAsync();
            }
        }
    }

    // Refuse to toggle off the last enabled region so the page never lands in the
    // "no region selected" empty state — users would otherwise tap a chip expecting
    // to focus that region and accidentally hide all results.
    private void ToggleBac()
    {
        if (ShowBac && !ShowTrung && !ShowNam) return;
        ShowBac = !ShowBac;
    }

    private void ToggleTrung()
    {
        if (ShowTrung && !ShowBac && !ShowNam) return;
        ShowTrung = !ShowTrung;
    }

    private void ToggleNam()
    {
        if (ShowNam && !ShowBac && !ShowTrung) return;
        ShowNam = !ShowNam;
    }

    // Live Button Dynamic Styles
    public string LiveButtonStartColor => IsAnyActiveRegionLive ? "#FF4D4D" : "#64748B";
    public string LiveButtonEndColor => IsAnyActiveRegionLive ? "#D90429" : "#334155";
    public string LiveButtonStrokeColor => IsAnyActiveRegionLive ? "#FFCCD5" : "#E2E8F0";
    public string LiveButtonShadowColor => IsAnyActiveRegionLive ? "#D90429" : "#475569";
    public string LiveButtonPulseColor => IsAnyActiveRegionLive ? "#4ADE80" : "#94A3B8";
    public string LiveButtonLabel => IsAnyActiveRegionLive ? "XEM LIVE" : "LIVE HÔM NAY";

    private void NotifyLiveButtonStyles()
    {
        NotifyPropertyChanged(nameof(IsAnyActiveRegionLive));
        NotifyPropertyChanged(nameof(LiveButtonStartColor));
        NotifyPropertyChanged(nameof(LiveButtonEndColor));
        NotifyPropertyChanged(nameof(LiveButtonStrokeColor));
        NotifyPropertyChanged(nameof(LiveButtonShadowColor));
        NotifyPropertyChanged(nameof(LiveButtonPulseColor));
        NotifyPropertyChanged(nameof(LiveButtonLabel));
    }

    private void NotifyChipStyles()
    {
        NotifyPropertyChanged(nameof(BacChipBg));
        NotifyPropertyChanged(nameof(BacChipText));
        NotifyPropertyChanged(nameof(BacChipStroke));
        NotifyPropertyChanged(nameof(TrungChipBg));
        NotifyPropertyChanged(nameof(TrungChipText));
        NotifyPropertyChanged(nameof(TrungChipStroke));
        NotifyPropertyChanged(nameof(NamChipBg));
        NotifyPropertyChanged(nameof(NamChipText));
        NotifyPropertyChanged(nameof(NamChipStroke));
        NotifyLiveButtonStyles();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Regions.Clear();
        foreach (var draw in allDraws.Where(d => !d.IsVietlott))
        {
            if (draw.Region == LotteryRegion.Bac && !ShowBac) continue;
            if (draw.Region == LotteryRegion.Trung && !ShowTrung) continue;
            if (draw.Region == LotteryRegion.Nam && !ShowNam) continue;

            Regions.Add(draw);
        }
        NotifyPropertyChanged(nameof(HasRegions));
        NotifyPropertyChanged(nameof(ShowEmptyState));
    }


    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        NotifyPropertyChanged(nameof(ShowEmptyState));
        try
        {
            var data = await resultsService.GetTodayResultsAsync(ct);
            allDraws = data ?? new List<LotteryRegionDraw>();
            
            ApplyFilter();

            UpdatedAtLabel = $"Cập nhật lúc {DateTime.Now:HH:mm}";
            
            var hasTodayData = allDraws.Any(d => d.DrawDate.Date == DateTime.Today);
            if (hasTodayData)
            {
                selectedDate = DateTime.Today;
                HeroDateLabel = DateTime.Today.ToString("dd 'tháng' MM, yyyy");
            }
            else
            {
                selectedDate = DateTime.Today.AddDays(-1);
                HeroDateLabel = DateTime.Today.AddDays(-1).ToString("dd 'tháng' MM, yyyy") + " (Hôm qua)";
            }
            NotifyPropertyChanged(nameof(SelectedDate));
            
            hasLoaded = true;
            NotifyLiveButtonStyles();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LotteryResults] InitializeAsync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NotifyPropertyChanged(nameof(ShowSkeleton));
            NotifyPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public async Task LoadDataForSelectedDateAsync()
    {
        IsBusy = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        NotifyPropertyChanged(nameof(ShowEmptyState));
        try
        {
            var data = await resultsService.GetResultsByDateAsync(SelectedDate);
            allDraws = data ?? new List<LotteryRegionDraw>();
            
            ApplyFilter();

            UpdatedAtLabel = $"Cập nhật lúc {DateTime.Now:HH:mm}";
            HeroDateLabel = SelectedDate.ToString("dd 'tháng' MM, yyyy");
            
            hasLoaded = true;
            NotifyLiveButtonStyles();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LotteryResults] LoadDataForSelectedDateAsync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NotifyPropertyChanged(nameof(ShowSkeleton));
            NotifyPropertyChanged(nameof(ShowEmptyState));
        }
    }
}
