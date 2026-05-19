using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Board;
using LotteryDetectionMobile.Models.Dashboard;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class DashboardViewModel : TabNavigationViewModel
{
    private readonly IDashboardRealtimeService? realtimeService;
    private readonly ITaskService taskService;
    private readonly IGamificationService? gamificationService;
    private readonly IAuthService? authService;
    private readonly INotificationService? notificationService;
    private readonly IFamilyMemberCache? memberCache;
    private readonly List<TodayTaskItem> allTasks = new();
    private string myMemberId = string.Empty;
    private string myMemberIdPublic = "home";

    private string filter = "all";
    private string greeting = "Good morning";
    private string greetingName = string.Empty;
    private bool hasLoadedData;
    private bool initialized;
    private TaskSummary? summary;
    private string? toast;
    private int levelXp;
    private int weekly;
    private int weeklyRank;
    private int streak;
    private int unreadCount;
    private string headlineCount = "0";
    private string headlineNote = string.Empty;
    private string pulseHeadline = string.Empty;
    private string pulseSubline = string.Empty;

    public DashboardViewModel()
        : this(NavigationService.Default, MockTaskService.Instance, null, null, null, null)
    {
    }

    public DashboardViewModel(
        INavigationService navigationService,
        ITaskService taskService,
        IDashboardRealtimeService? realtimeService = null,
        IGamificationService? gamificationService = null,
        IAuthService? authService = null,
        INotificationService? notificationService = null,
        IFamilyMemberCache? memberCache = null)
        : base(navigationService)
    {
        this.taskService = taskService;
        this.realtimeService = realtimeService;
        this.gamificationService = gamificationService;
        this.authService = authService;
        this.notificationService = notificationService;
        this.memberCache = memberCache;
        Highlights = new ObservableCollection<string>();
        TodayTasks = new ObservableCollection<TaskItem>();
        FilteredTasks = new ObservableCollection<TodayTaskItem>();
        WeekDays = new ObservableCollection<Models.Dashboard.DayCell>();
        TodayLineup = new ObservableCollection<TodayTaskItem>();
        PulseMembers = new ObservableCollection<PresenceMember>();

        OpenAssistantCommand = new Command(async () => await navigationService.NavigateToAITaskAssistantAsync());
        OpenChatToTaskCommand = new Command(async () => await navigationService.NavigateToChatToTaskAsync());
        OpenGamificationCommand = new Command(async () => await navigationService.NavigateToGamificationAsync());
        OpenAchievementsCommand = OpenGamificationCommand;
        OpenNotificationsCommand = new Command(async () => await navigationService.NavigateToNotificationsAsync());
        OpenBoardCommand = new Command(async () => await navigationService.NavigateToFamilyBoardAsync());
        OpenCalendarCommand = new Command(async () => await navigationService.NavigateToCalendarAsync());
        ViewAllTasksCommand = new Command(async () => await navigationService.NavigateToMyTasksAsync());
        OpenLotteryCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
        SetFilterCommand = new Command<string>(SetFilter);
        ToggleTaskCommand = new Command<string>(ToggleTask);
        SelectTaskCommand = new Command<TodayTaskItem>(async t => await navigationService.NavigateToTaskDetailAsync(t.Id));

        BuildWeekDays();
    }

    public TaskSummary? Summary
    {
        get => summary;
        private set => SetProperty(ref summary, value);
    }

    public ObservableCollection<string> Highlights { get; }
    public ObservableCollection<TaskItem> TodayTasks { get; }
    public ObservableCollection<TodayTaskItem> FilteredTasks { get; }
    public ObservableCollection<Models.Dashboard.DayCell> WeekDays { get; }
    public ObservableCollection<TodayTaskItem> TodayLineup { get; }
    public ObservableCollection<PresenceMember> PulseMembers { get; }

    public string MyMemberId
    {
        get => myMemberIdPublic;
        private set => SetProperty(ref myMemberIdPublic, value);
    }

    public string DateLabel => DateTime.Now.ToString("ddd · MMM d");

    public string GreetingInitial => string.IsNullOrEmpty(GreetingName) ? "?" : GreetingName[..1];

    public string HeadlineCount
    {
        get => headlineCount;
        private set => SetProperty(ref headlineCount, value);
    }

    public string HeadlineNote
    {
        get => headlineNote;
        private set => SetProperty(ref headlineNote, value);
    }

    public int UnreadCount
    {
        get => unreadCount;
        set
        {
            if (SetProperty(ref unreadCount, value))
            {
                NotifyPropertyChanged(nameof(HasUnread));
                NotifyPropertyChanged(nameof(UnreadLabel));
            }
        }
    }

    public bool HasUnread => unreadCount > 0;

    public string UnreadLabel => unreadCount > 9 ? "9+" : unreadCount.ToString();

    public string SummaryDueToday => Summary?.DueToday.ToString() ?? "0";

    public string SummaryOverdue => Summary?.OpenTasks > Summary?.DueToday
        ? (Summary!.OpenTasks - Summary.DueToday).ToString()
        : "0";

    public string SummaryDoneWeek => Summary?.Completed.ToString() ?? "0";

    public string PulseHeadline
    {
        get => pulseHeadline;
        private set => SetProperty(ref pulseHeadline, value);
    }

    public string PulseSubline
    {
        get => pulseSubline;
        private set => SetProperty(ref pulseSubline, value);
    }

    public bool HasPulse => !string.IsNullOrEmpty(pulseHeadline);

    public bool HasTodayTasks => TodayTasks.Count > 0;

    public bool HasTodayLineup => TodayLineup.Count > 0;

    public bool HasWeekTasks => WeekDays.Any(d => d.DotCount > 0);

    public bool ShowSkeleton => IsBusy && !hasLoadedData;

    public string Greeting
    {
        get => greeting;
        set => SetProperty(ref greeting, value);
    }

    public string GreetingName
    {
        get => greetingName;
        set => SetProperty(ref greetingName, value);
    }

    public string GreetingHeadline => $"{Greeting}, {GreetingName}";

    public string FilterSubtitle => $"{FilteredTasks.Count} {(filter == "done" ? "completed" : "tasks")} · {(filter == "today" ? "today" : "this week")}";

    public string Filter
    {
        get => filter;
        private set
        {
            if (SetProperty(ref filter, value))
            {
                NotifyPropertyChanged(nameof(IsFilterAll));
                NotifyPropertyChanged(nameof(IsFilterToday));
                NotifyPropertyChanged(nameof(IsFilterMine));
                NotifyPropertyChanged(nameof(IsFilterDone));
                NotifyPropertyChanged(nameof(FilterSubtitle));
            }
        }
    }

    public bool IsFilterAll => filter == "all";
    public bool IsFilterToday => filter == "today";
    public bool IsFilterMine => filter == "mine";
    public bool IsFilterDone => filter == "done";

    public string? Toast
    {
        get => toast;
        private set
        {
            if (SetProperty(ref toast, value))
                NotifyPropertyChanged(nameof(ShowToast));
        }
    }

    public bool ShowToast => !string.IsNullOrEmpty(toast);

    public int LevelXp
    {
        get => levelXp;
        set => SetProperty(ref levelXp, value);
    }

    public int Weekly
    {
        get => weekly;
        set => SetProperty(ref weekly, value);
    }

    public int WeeklyRank
    {
        get => weeklyRank;
        set => SetProperty(ref weeklyRank, value);
    }

    public int Streak
    {
        get => streak;
        set => SetProperty(ref streak, value);
    }

    public ICommand OpenAssistantCommand { get; }
    public ICommand OpenChatToTaskCommand { get; }
    public ICommand OpenGamificationCommand { get; }
    public ICommand OpenAchievementsCommand { get; }
    public ICommand OpenNotificationsCommand { get; }
    public ICommand OpenBoardCommand { get; }
    public ICommand OpenCalendarCommand { get; }
    public ICommand ViewAllTasksCommand { get; }
    public ICommand OpenLotteryCaptureCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand ToggleTaskCommand { get; }
    public ICommand SelectTaskCommand { get; }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    public async Task InitializeAsync()
    {
        Greeting = ResolveGreeting(DateTime.Now);
        GreetingName = ResolveGreetingName();
        NotifyPropertyChanged(nameof(GreetingHeadline));
        NotifyPropertyChanged(nameof(GreetingInitial));
        NotifyPropertyChanged(nameof(DateLabel));

        if (memberCache != null)
        {
            try
            {
                var members = await memberCache.GetMembersAsync();
                var userEmail = authService?.UserEmail ?? string.Empty;
                var myMember = members.FirstOrDefault(m =>
                    string.Equals(m.Email, userEmail, StringComparison.OrdinalIgnoreCase));
                myMemberId = myMember?.Id ?? string.Empty;

                if (!string.IsNullOrEmpty(myMemberId))
                    MyMemberId = myMemberId;

                if (!string.IsNullOrWhiteSpace(myMember?.Name))
                {
                    GreetingName = myMember.Name;
                    Preferences.Set("user_display_name_override", myMember.Name);
                    NotifyPropertyChanged(nameof(GreetingHeadline));
                    NotifyPropertyChanged(nameof(GreetingInitial));
                }

                PulseMembers.Clear();
                foreach (var m in members.Take(3))
                {
                    var initial = string.IsNullOrWhiteSpace(m.Name) ? "?" : m.Name[..1].ToUpperInvariant();
                    PulseMembers.Add(new PresenceMember { Member = m.Id, Initial = initial });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Dashboard] MemberCache lookup error: {ex.Message}");
            }
        }

        IsBusy = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        try
        {
            var snapshot = await taskService.GetDashboardSummaryAsync();
            Summary = snapshot;

            Highlights.Clear();
            foreach (var item in snapshot.Highlights)
                Highlights.Add(item);

            var board = await taskService.GetBoardTasksAsync();
            allTasks.Clear();
            foreach (var task in board)
                allTasks.Add(MapToToday(task));

            // Map five "today" rows for legacy DashboardPage callers (HasTodayTasks).
            TodayTasks.Clear();
            foreach (var task in board.Take(5))
                TodayTasks.Add(task);

            ApplyFilter();
            BuildTodayLineup();
            BuildWeekDays();
            BuildPulse();
            NotifyPropertyChanged(nameof(HasTodayTasks));
            NotifyPropertyChanged(nameof(SummaryDueToday));
            NotifyPropertyChanged(nameof(SummaryOverdue));
            NotifyPropertyChanged(nameof(SummaryDoneWeek));
            hasLoadedData = true;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Dashboard] InitializeAsync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }

        await LoadGamificationAsync();
        await LoadUnreadCountAsync();

        if (!initialized)
        {
            initialized = true;
            if (realtimeService != null)
            {
                realtimeService.DashboardUpdated += OnDashboardUpdated;
                await realtimeService.ConnectAsync();
            }
        }
    }

    private async Task LoadGamificationAsync()
    {
        if (gamificationService == null) return;
        try
        {
            var points = await gamificationService.GetCurrentPointsAsync();
            var streakResult = await gamificationService.GetCurrentStreakAsync();

            LevelXp = points;
            Weekly = points; // YAGNI: shared source until weekly endpoint exists
            Streak = streakResult?.Days ?? 0;

            var leaderboard = (await gamificationService.GetLeaderboardAsync())?.ToList()
                              ?? new List<FamilyMember>();

            // Best-effort rank — leaderboard IsMe flag lives server-side but not surfaced here yet.
            // v1: WeeklyRank stays 0 ("—" in UI) until IAuthService exposes oid.
            WeeklyRank = 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Dashboard] LoadGamificationAsync error: {ex.Message}");
        }
    }

    private async Task LoadUnreadCountAsync()
    {
        if (notificationService == null) return;
        try
        {
            UnreadCount = await notificationService.GetUnreadCountAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Dashboard] LoadUnreadCountAsync error: {ex.Message}");
        }
    }

    private string ResolveGreetingName()
    {
        // Prefer the display name saved by Settings (server-synced, user-editable).
        var cached = Preferences.Get("user_display_name_override", string.Empty);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        var raw = authService?.UserDisplayName;
        if (string.IsNullOrWhiteSpace(raw)) return "there";

        // EntraIdAuthService returns email-localpart (e.g. "alex.chen"). Take first segment, capitalize.
        var segment = raw.Split(new[] { '.', ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(segment)) return "there";
        return char.ToUpperInvariant(segment[0]) + segment[1..];
    }

    private TodayTaskItem MapToToday(TaskItem task)
    {
        var member = !string.IsNullOrEmpty(task.AssigneeId) ? task.AssigneeId : "home";

        var when = "Today";
        var isToday = false;
        if (task.DueDate.HasValue)
        {
            var d = task.DueDate.Value;
            isToday = d.Date == DateTime.Today;
            when = isToday
                ? $"Today · {d:h:mm tt}"
                : d.Date == DateTime.Today.AddDays(1)
                    ? $"Tomorrow · {d:h:mm tt}"
                    : $"{d:ddd} · {d:h:mm tt}";
        }

        var priority = (task.Priority ?? "Medium").ToLowerInvariant() switch
        {
            "high" => "high",
            "low" => "low",
            _ => "med"
        };

        var done = string.Equals(task.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(task.Status, "Done", StringComparison.OrdinalIgnoreCase);

        var points = task.Points > 0 ? task.Points : priority switch
        {
            "high" => 30,
            "low" => 10,
            _ => 20
        };

        var ownerLabel = string.IsNullOrEmpty(task.Owner)
            ? "Shared"
            : char.ToUpperInvariant(task.Owner[0]) + task.Owner[1..];

        return new TodayTaskItem
        {
            Id = task.Id,
            Title = task.Title,
            Subtitle = task.Description,
            OwnerLabel = ownerLabel,
            Member = member,
            When = when,
            Priority = priority,
            Points = points,
            Done = done,
            IsToday = isToday
        };
    }

    private static string ResolveGreeting(DateTime now)
    {
        return now.Hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };
    }

    private void SetFilter(string? key)
    {
        if (string.IsNullOrEmpty(key)) return;
        Filter = key.ToLowerInvariant();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredTasks.Clear();
        IEnumerable<TodayTaskItem> source = filter switch
        {
            "done" => allTasks.Where(t => t.Done),
            "today" => allTasks.Where(t => !t.Done && t.IsToday),
            "mine" => allTasks.Where(t => !t.Done && !string.IsNullOrEmpty(myMemberId) && t.Member == myMemberId),
            _ => allTasks.Where(t => !t.Done)
        };
        foreach (var t in source)
            FilteredTasks.Add(t);

        NotifyPropertyChanged(nameof(FilterSubtitle));
    }

    private void ToggleTask(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var match = allTasks.FirstOrDefault(t => t.Id == id);
        if (match == null) return;

        var wasUndone = !match.Done;
        match.Done = !match.Done;
        ApplyFilter();

        if (wasUndone)
        {
            LevelXp += match.Points;
            Weekly += match.Points;
            ShowToastFor($"+{match.Points} XP earned");
            NotifyPropertyChanged(nameof(LevelXp));
            NotifyPropertyChanged(nameof(Weekly));
        }
    }

    private async void ShowToastFor(string message)
    {
        Toast = message;
        try
        {
            await Task.Delay(2400);
            if (Toast == message) Toast = null;
        }
        catch
        {
            // ignore
        }
    }

    private void OnDashboardUpdated(object? sender, DashboardMetrics metrics)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Summary = new TaskSummary
            {
                OpenTasks = metrics.OpenTasks,
                DueToday = metrics.DueToday,
                Completed = metrics.Completed,
                Highlights = Summary?.Highlights ?? new List<string>()
            };
            NotifyPropertyChanged(nameof(SummaryDueToday));
            NotifyPropertyChanged(nameof(SummaryOverdue));
            NotifyPropertyChanged(nameof(SummaryDoneWeek));
        });
    }

    private void BuildTodayLineup()
    {
        TodayLineup.Clear();
        var today = allTasks.Where(t => !t.Done && t.IsToday).Take(5);
        foreach (var t in today) TodayLineup.Add(t);

        HeadlineCount = TodayLineup.Count == 0 && allTasks.Count(a => !a.Done) > 0
            ? allTasks.Count(a => !a.Done).ToString()
            : TodayLineup.Count.ToString();

        HeadlineNote = Summary?.Highlights?.FirstOrDefault() ?? "Quiet day · enjoy.";
        // Count is not auto-observed on ObservableCollection — notify the dependent flag so empty-state visibility flips.
        NotifyPropertyChanged(nameof(HasTodayLineup));
    }

    private void BuildWeekDays()
    {
        WeekDays.Clear();
        // Bundle locale: week starts Monday (VN locale per CLAUDE.md cross-cutting).
        var today = DateTime.Today;
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = today.AddDays(-diff);
        var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        for (var i = 0; i < 7; i++)
        {
            var d = monday.AddDays(i);
            var dotCount = allTasks.Count(t => t.IsToday && d.Date == DateTime.Today)
                + (d.Date == DateTime.Today ? 0 : 0); // placeholder until per-day data lives in VM
            // Approximate: count allTasks whose When references that weekday name (best-effort with current schema)
            var dayName = d.ToString("ddd");
            if (d.Date != DateTime.Today)
                dotCount = allTasks.Count(t => !t.Done && t.When.Contains(dayName, StringComparison.OrdinalIgnoreCase));

            WeekDays.Add(new Models.Dashboard.DayCell
            {
                Label = labels[i],
                Day = d.Day,
                IsToday = d.Date == DateTime.Today,
                DotCount = Math.Min(dotCount, 4),
                Date = d
            });
        }

        NotifyPropertyChanged(nameof(HasWeekTasks));
    }

    private void BuildPulse()
    {
        // First non-done task surfaces as pulse for now — replace with realtime activity feed when wired.
        var sample = allTasks.FirstOrDefault(t => !t.Done);
        if (sample == null)
        {
            PulseHeadline = string.Empty;
            PulseSubline = string.Empty;
        }
        else
        {
            PulseHeadline = $"{sample.OwnerLabel} · {sample.Title}";
            PulseSubline = $"{sample.When}";
        }
        NotifyPropertyChanged(nameof(HasPulse));
    }
}
