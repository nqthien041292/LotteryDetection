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

        // Load local preferences
        showBac = Microsoft.Maui.Storage.Preferences.Get("ShowRegionBac", true);
        showTrung = Microsoft.Maui.Storage.Preferences.Get("ShowRegionTrung", true);
        showNam = Microsoft.Maui.Storage.Preferences.Get("ShowRegionNam", true);

        ToggleBacCommand = new Command(ToggleBac);
        ToggleTrungCommand = new Command(ToggleTrung);
        ToggleNamCommand = new Command(ToggleNam);
    }

    public ObservableCollection<LotteryRegionDraw> Regions { get; }
    public ICommand OpenCaptureCommand { get; }
    public ICommand ToggleBacCommand { get; }
    public ICommand ToggleTrungCommand { get; }
    public ICommand ToggleNamCommand { get; }

    public bool HasRegions => Regions.Count > 0;
    public bool ShowSkeleton => IsBusy && !hasLoaded;
    public bool ShowEmptyState => !HasRegions && !IsBusy;

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

    private void ToggleBac() => ShowBac = !ShowBac;
    private void ToggleTrung() => ShowTrung = !ShowTrung;
    private void ToggleNam() => ShowNam = !ShowNam;

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
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Regions.Clear();
        foreach (var draw in allDraws)
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
                HeroDateLabel = DateTime.Today.ToString("dd 'tháng' MM, yyyy");
            }
            else
            {
                HeroDateLabel = DateTime.Today.AddDays(-1).ToString("dd 'tháng' MM, yyyy") + " (Hôm qua)";
            }
            
            hasLoaded = true;
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
}
