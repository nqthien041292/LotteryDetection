using System.Collections.ObjectModel;
using System.Windows.Input;
using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class NotificationsViewModel : TabNavigationViewModel
{
    public static readonly string[] Filters = { "All", "Task", "Calendar", "Conflict" };

    private readonly INotificationService notificationService;
    private readonly List<NotificationItem> source = new();
    private bool hasLoaded;
    private bool isLoading;
    private string selectedFilter = "All";

    public NotificationsViewModel()
        : this(NavigationService.Default, MockNotificationService.Instance)
    {
    }

    public NotificationsViewModel(INavigationService navigationService, INotificationService notificationService)
        : base(navigationService)
    {
        this.notificationService = notificationService;
        Notifications = new ObservableCollection<NotificationItem>();

        SetFilterCommand = new Command<string>(filter =>
        {
            if (string.IsNullOrEmpty(filter)) return;
            SelectedFilter = filter;
        });
        DismissCommand = new Command<NotificationItem>(Dismiss);
        MarkReadCommand = new Command<NotificationItem>(MarkRead);
        MarkAllReadCommand = new Command(MarkAllRead);
        ClearAllCommand = new Command(ClearAll);
    }

    public ObservableCollection<NotificationItem> Notifications { get; }

    public ICommand SetFilterCommand { get; }
    public ICommand DismissCommand { get; }
    public ICommand MarkReadCommand { get; }
    public ICommand MarkAllReadCommand { get; }
    public ICommand ClearAllCommand { get; }

    public string SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (SetProperty(ref selectedFilter, value))
            {
                ApplyFilter();
                NotifyFilterStates();
            }
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        set
        {
            if (SetProperty(ref isLoading, value))
                NotifyPropertyChanged(nameof(ShowSkeleton));
        }
    }

    public bool ShowSkeleton => IsLoading && !hasLoaded;

    public bool IsAllSelected => SelectedFilter == "All";
    public bool IsTaskSelected => SelectedFilter == "Task";
    public bool IsCalendarSelected => SelectedFilter == "Calendar";
    public bool IsConflictSelected => SelectedFilter == "Conflict";

    public int CountAll => source.Count;
    public int CountTask => source.Count(n => n.Category == "Task");
    public int CountCalendar => source.Count(n => n.Category == "Calendar");
    public int CountConflict => source.Count(n => n.Category == "Conflict");
    public int UnreadCount => source.Count(n => n.IsUnread);

    public string AllChipLabel => CountAll > 0 ? $"All  {CountAll}" : "All";
    public string TaskChipLabel => CountTask > 0 ? $"Tasks  {CountTask}" : "Tasks";
    public string CalendarChipLabel => CountCalendar > 0 ? $"Calendar  {CountCalendar}" : "Calendar";
    public string ConflictChipLabel => CountConflict > 0 ? $"Conflicts  {CountConflict}" : "Conflicts";

    public string UnreadHeader => UnreadCount > 0 ? $"{UnreadCount} unread" : "All caught up";
    public string UnreadSubtitle => UnreadCount > 0 ? "Swipe a card left to dismiss" : "No new notifications";
    public bool HasUnread => UnreadCount > 0;

    public bool HasItems => Notifications.Count > 0;
    public bool IsEmpty => !HasItems;

    public async Task InitializeAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        source.Clear();
        var items = await notificationService.GetNotificationsAsync();
        source.AddRange(items);
        ApplyFilter();
        NotifyCounts();
        NotifyFilterStates();
        hasLoaded = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        IsLoading = false;
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    private void ApplyFilter()
    {
        Notifications.Clear();
        var filtered = SelectedFilter == "All"
            ? source
            : source.Where(n => n.Category == SelectedFilter);
        foreach (var item in filtered) Notifications.Add(item);
        NotifyPropertyChanged(nameof(HasItems));
        NotifyPropertyChanged(nameof(IsEmpty));
    }

    private void Dismiss(NotificationItem? item)
    {
        if (item == null) return;
        source.Remove(item);
        Notifications.Remove(item);
        NotifyCounts();
        NotifyPropertyChanged(nameof(HasItems));
        NotifyPropertyChanged(nameof(IsEmpty));
    }

    private void MarkRead(NotificationItem? item)
    {
        if (item == null) return;
        item.IsUnread = false;
        _ = notificationService.MarkAsReadAsync(item.Id.ToString());
        NotifyCounts();
    }

    private void MarkAllRead()
    {
        foreach (var n in source) n.IsUnread = false;
        NotifyCounts();
    }

    private void ClearAll()
    {
        source.Clear();
        Notifications.Clear();
        NotifyCounts();
        NotifyPropertyChanged(nameof(HasItems));
        NotifyPropertyChanged(nameof(IsEmpty));
    }

    private void NotifyCounts()
    {
        NotifyPropertyChanged(nameof(CountAll));
        NotifyPropertyChanged(nameof(CountTask));
        NotifyPropertyChanged(nameof(CountCalendar));
        NotifyPropertyChanged(nameof(CountConflict));
        NotifyPropertyChanged(nameof(UnreadCount));
        NotifyPropertyChanged(nameof(AllChipLabel));
        NotifyPropertyChanged(nameof(TaskChipLabel));
        NotifyPropertyChanged(nameof(CalendarChipLabel));
        NotifyPropertyChanged(nameof(ConflictChipLabel));
        NotifyPropertyChanged(nameof(UnreadHeader));
        NotifyPropertyChanged(nameof(UnreadSubtitle));
        NotifyPropertyChanged(nameof(HasUnread));
    }

    private void NotifyFilterStates()
    {
        NotifyPropertyChanged(nameof(IsAllSelected));
        NotifyPropertyChanged(nameof(IsTaskSelected));
        NotifyPropertyChanged(nameof(IsCalendarSelected));
        NotifyPropertyChanged(nameof(IsConflictSelected));
    }
}
