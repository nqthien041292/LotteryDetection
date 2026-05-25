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

    private LotteryHistoryEntry? _selectedTicket;
    private bool _isTicketDetailsOpen;
    private bool _isLoadingMore;

    private const int PageSize = 5;
    private int _currentSkipCount = 0;

    public LotteryHistoryViewModel(ILotteryHistoryService historyService, INavigationService navigationService)
    {
        this.historyService = historyService;
        this.navigationService = navigationService;
        Entries = new ObservableCollection<LotteryHistoryEntry>();
        Winners = new ObservableCollection<LotteryHistoryEntry>();
        
        StartCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
        DeleteEntryCommand = new Command<LotteryHistoryEntry>(async (entry) => await DeleteEntryAsync(entry));
        LoadMoreCommand = new Command(async () => await LoadMoreAsync());
        OpenTicketDetailsCommand = new Command<LotteryHistoryEntry>(OnOpenTicketDetails);
        CloseTicketDetailsCommand = new Command(OnCloseTicketDetails);
    }

    public ObservableCollection<LotteryHistoryEntry> Entries { get; }
    public ObservableCollection<LotteryHistoryEntry> Winners { get; }
    
    public ICommand StartCaptureCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand OpenTicketDetailsCommand { get; }
    public ICommand CloseTicketDetailsCommand { get; }

    public bool HasEntries => Entries.Count > 0;
    public bool HasWinners => Winners.Count > 0;
    public bool ShowEmptyState => !IsBusy && hasLoaded && Entries.Count == 0;
    public bool ShowSkeleton => IsBusy && !hasLoaded;

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public bool CanLoadMore => Entries.Count < TotalCount && !IsBusy && !IsLoadingMore;

    public LotteryHistoryEntry? SelectedTicket
    {
        get => _selectedTicket;
        set => SetProperty(ref _selectedTicket, value);
    }

    public bool IsTicketDetailsOpen
    {
        get => _isTicketDetailsOpen;
        set => SetProperty(ref _isTicketDetailsOpen, value);
    }

    public int WinCount
    {
        get => winCount;
        private set => SetProperty(ref winCount, value);
    }

    public int TotalCount
    {
        get => totalCount;
        private set
        {
            if (SetProperty(ref totalCount, value))
                NotifyPropertyChanged(nameof(CanLoadMore));
        }
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
            _currentSkipCount = 0;
            Entries.Clear();
            Winners.Clear();

            // 1. Fetch Stats from DB (accurately counts totals)
            var stats = await historyService.GetStatsAsync(ct);
            if (stats != null)
            {
                TotalCount = stats.TotalCount;
                WinCount = stats.WinCount;
                TotalWinnings = stats.TotalWinnings;
                BiggestWin = stats.BiggestWin;
            }

            // 2. Fetch First Page of Entries
            var pageResult = await historyService.GetEntriesAsync(_currentSkipCount, PageSize, ct);
            if (pageResult != null)
            {
                // In case stats wasn't returned, update count as fallback
                if (stats == null)
                {
                    TotalCount = pageResult.TotalCount;
                }

                foreach (var entry in pageResult.Items)
                {
                    Entries.Add(entry);
                    if (entry.IsWinner)
                    {
                        Winners.Add(entry);
                    }
                }
            }

            hasLoaded = true;
            UpdateUiProperties();
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
            NotifyPropertyChanged(nameof(CanLoadMore));
        }
    }

    public async Task LoadMoreAsync(CancellationToken ct = default)
    {
        if (!CanLoadMore) return;

        IsLoadingMore = true;
        NotifyPropertyChanged(nameof(CanLoadMore));

        try
        {
            _currentSkipCount += PageSize;
            var pageResult = await historyService.GetEntriesAsync(_currentSkipCount, PageSize, ct);
            if (pageResult != null && pageResult.Items.Any())
            {
                foreach (var entry in pageResult.Items)
                {
                    // Avoid duplicate records
                    if (!Entries.Any(e => e.Id == entry.Id))
                    {
                        Entries.Add(entry);
                        if (entry.IsWinner && !Winners.Any(w => w.Id == entry.Id))
                        {
                            Winners.Add(entry);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LotteryHistory] LoadMoreAsync error: {ex.Message}");
            _currentSkipCount -= PageSize; // rollback on failure
        }
        finally
        {
            IsLoadingMore = false;
            NotifyPropertyChanged(nameof(CanLoadMore));
            NotifyPropertyChanged(nameof(HasEntries));
            NotifyPropertyChanged(nameof(HasWinners));
        }
    }

    private void OnOpenTicketDetails(LotteryHistoryEntry entry)
    {
        if (entry == null) return;
        SelectedTicket = entry;
        IsTicketDetailsOpen = true;
    }

    private void OnCloseTicketDetails()
    {
        IsTicketDetailsOpen = false;
        SelectedTicket = null;
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

            // Fetch updated stats to remain accurate
            var stats = await historyService.GetStatsAsync();
            if (stats != null)
            {
                TotalCount = stats.TotalCount;
                WinCount = stats.WinCount;
                TotalWinnings = stats.TotalWinnings;
                BiggestWin = stats.BiggestWin;
            }
            else
            {
                TotalCount = Math.Max(0, TotalCount - 1);
                if (entry.IsWinner)
                {
                    WinCount = Math.Max(0, WinCount - 1);
                    TotalWinnings = Winners.Sum(w => w.PrizeAmount ?? 0);
                    BiggestWin = Winners.Count > 0 ? Winners.Max(w => w.PrizeAmount ?? 0L) : 0;
                }
            }

            UpdateUiProperties();
        }
        return success;
    }

    private void UpdateUiProperties()
    {
        NotifyPropertyChanged(nameof(HasEntries));
        NotifyPropertyChanged(nameof(HasWinners));
        NotifyPropertyChanged(nameof(ShowEmptyState));
        NotifyPropertyChanged(nameof(SummaryLabel));
        NotifyPropertyChanged(nameof(HeroHeadline));
        NotifyPropertyChanged(nameof(HeroSubline));
        NotifyPropertyChanged(nameof(CanLoadMore));
    }
}
