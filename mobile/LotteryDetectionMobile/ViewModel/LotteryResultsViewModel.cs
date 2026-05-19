using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Lottery;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class LotteryResultsViewModel : BaseViewModel
{
    private readonly ILotteryResultsService resultsService;
    private readonly INavigationService navigationService;
    private string updatedAtLabel = string.Empty;
    private string heroDateLabel = string.Empty;
    private bool hasLoaded;

    public LotteryResultsViewModel(ILotteryResultsService resultsService, INavigationService navigationService)
    {
        this.resultsService = resultsService;
        this.navigationService = navigationService;
        Regions = new ObservableCollection<LotteryRegionDraw>();
        OpenCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
    }

    public ObservableCollection<LotteryRegionDraw> Regions { get; }
    public ICommand OpenCaptureCommand { get; }

    public bool HasRegions => Regions.Count > 0;
    public bool ShowSkeleton => IsBusy && !hasLoaded;

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

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        try
        {
            var data = await resultsService.GetTodayResultsAsync(ct);
            Regions.Clear();
            foreach (var draw in data)
                Regions.Add(draw);

            UpdatedAtLabel = $"Cập nhật lúc {DateTime.Now:HH:mm}";
            HeroDateLabel = DateTime.Today.ToString("dd 'tháng' MM, yyyy");
            hasLoaded = true;
            NotifyPropertyChanged(nameof(HasRegions));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LotteryResults] InitializeAsync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
    }
}
