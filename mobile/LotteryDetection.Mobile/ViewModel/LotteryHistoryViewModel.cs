using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class LotteryHistoryViewModel : BaseViewModel
{
    private readonly ILotteryHistoryService historyService;
    private readonly INavigationService navigationService;
    private bool hasLoaded;
    private int winCount;
    private int totalCount;
    private long totalWinnings;
    private long biggestWin;

    public LotteryHistoryViewModel(ILotteryHistoryService historyService, INavigationService navigationService)
    {
        this.historyService = historyService;
        this.navigationService = navigationService;
        Entries = new ObservableCollection<LotteryHistoryEntry>();
        Winners = new ObservableCollection<LotteryHistoryEntry>();
        StartCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
        DeleteEntryCommand = new Command<LotteryHistoryEntry>(async (entry) => await DeleteEntryAsync(entry));
    }

    public ObservableCollection<LotteryHistoryEntry> Entries { get; }
    public ObservableCollection<LotteryHistoryEntry> Winners { get; }
    public ICommand StartCaptureCommand { get; }
    public ICommand DeleteEntryCommand { get; }

    public bool HasEntries => Entries.Count > 0;
    public bool HasWinners => Winners.Count > 0;
    public bool ShowEmptyState => !IsBusy && hasLoaded && Entries.Count == 0;
    public bool ShowSkeleton => IsBusy && !hasLoaded;

    public int WinCount
    {
        get => winCount;
        private set => SetProperty(ref winCount, value);
    }

    public int TotalCount
    {
        get => totalCount;
        private set => SetProperty(ref totalCount, value);
    }

    public long TotalWinnings
    {
        get => totalWinnings;
        private set
        {
            if (SetProperty(ref totalWinnings, value))
                NotifyPropertyChanged(nameof(TotalWinningsDisplay));
        }
    }

    public long BiggestWin
    {
        get => biggestWin;
        private set
        {
            if (SetProperty(ref biggestWin, value))
                NotifyPropertyChanged(nameof(BiggestWinDisplay));
        }
    }

    public string TotalWinningsDisplay => totalWinnings > 0 ? $"{totalWinnings:N0} đ" : "—";
    public string BiggestWinDisplay => biggestWin > 0 ? $"{biggestWin:N0} đ" : "—";

    public string SummaryLabel => $"{TotalCount} vé đã dò · {WinCount} trúng giải";
    public string HeroHeadline => WinCount > 0
        ? $"Bạn đã trúng {WinCount} vé"
        : "Chưa có vé trúng";
    public string HeroSubline => WinCount > 0
        ? "Tổng thưởng tích lũy gần đây"
        : "Tiếp tục dò để săn giải may mắn";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        try
        {
            var data = await historyService.GetEntriesAsync(ct);
            Entries.Clear();
            Winners.Clear();
            foreach (var entry in data)
            {
                Entries.Add(entry);
                if (entry.IsWinner) Winners.Add(entry);
            }

            TotalCount = Entries.Count;
            WinCount = Winners.Count;
            TotalWinnings = Winners.Sum(w => w.PrizeAmount ?? 0);
            BiggestWin = Winners.Count > 0 ? Winners.Max(w => w.PrizeAmount ?? 0L) : 0;
            hasLoaded = true;
            NotifyPropertyChanged(nameof(HasEntries));
            NotifyPropertyChanged(nameof(HasWinners));
            NotifyPropertyChanged(nameof(ShowEmptyState));
            NotifyPropertyChanged(nameof(SummaryLabel));
            NotifyPropertyChanged(nameof(HeroHeadline));
            NotifyPropertyChanged(nameof(HeroSubline));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LotteryHistory] InitializeAsync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            hasLoaded = true;
            NotifyPropertyChanged(nameof(ShowSkeleton));
            NotifyPropertyChanged(nameof(ShowEmptyState));
            NotifyPropertyChanged(nameof(HasEntries));
            NotifyPropertyChanged(nameof(HasWinners));
        }
    }

    public async Task<bool> DeleteEntryAsync(LotteryHistoryEntry entry)
    {
        if (entry == null) return false;

        bool success = await historyService.DeleteEntryAsync(entry.Id);
        if (success)
        {
            // Remove from main list
            var mainItem = Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (mainItem != null) Entries.Remove(mainItem);

            // Remove from winners if it is a winner
            var winnerItem = Winners.FirstOrDefault(w => w.Id == entry.Id);
            if (winnerItem != null) Winners.Remove(winnerItem);

            // Update stats
            TotalCount = Entries.Count;
            WinCount = Winners.Count;
            TotalWinnings = Winners.Sum(w => w.PrizeAmount ?? 0);
            BiggestWin = Winners.Count > 0 ? Winners.Max(w => w.PrizeAmount ?? 0L) : 0;

            NotifyPropertyChanged(nameof(HasEntries));
            NotifyPropertyChanged(nameof(HasWinners));
            NotifyPropertyChanged(nameof(ShowEmptyState));
            NotifyPropertyChanged(nameof(SummaryLabel));
            NotifyPropertyChanged(nameof(HeroHeadline));
            NotifyPropertyChanged(nameof(HeroSubline));
        }
        return success;
    }
}
