using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Calendar;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class CalendarViewModel : TabNavigationViewModel
{
    private const string ModePrefKey = "calendar.mode";

    private readonly ICalendarService calendarService;
    private readonly IFamilyMemberCache? memberCache;
    private readonly List<CalendarEvent> events = new();
    private Dictionary<string, string> memberNameById = new();
    private bool hasLoaded;
    private bool isLoading;
    private string mode = "month";
    private DateTime selectedDate = DateTime.Today;
    private DateTime monthAnchor = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private SyncState syncState = new() { LastSyncAt = DateTime.Now.AddMinutes(-2) };

    public CalendarViewModel()
        : this(NavigationService.Default, MockCalendarService.Instance)
    {
    }

    public CalendarViewModel(INavigationService navigationService, ICalendarService calendarService,
        IFamilyMemberCache? memberCache = null)
        : base(navigationService)
    {
        this.calendarService = calendarService;
        this.memberCache = memberCache;
        Events = new ObservableCollection<CalendarEvent>();
        MonthCells = new ObservableCollection<MonthCell>();
        WeekDays = new ObservableCollection<WeekDayHeader>();
        WeekBlocks = new ObservableCollection<WeekEventBlock>();
        SelectedDayEvents = new ObservableCollection<CalendarEvent>();
        LegendMembers = new ObservableCollection<CalendarLegendMember>();

        ToggleModeCommand = new Command<string>(SetMode);
        SelectDayCommand = new Command<MonthCell>(SelectDay);
        PrevMonthCommand = new Command(() => ShiftMonth(-1));
        NextMonthCommand = new Command(() => ShiftMonth(+1));

        mode = Preferences.Default.Get(ModePrefKey, "month");
    }

    public ObservableCollection<CalendarEvent> Events { get; }
    public ObservableCollection<MonthCell> MonthCells { get; }
    public ObservableCollection<WeekDayHeader> WeekDays { get; }
    public ObservableCollection<WeekEventBlock> WeekBlocks { get; }
    public ObservableCollection<CalendarEvent> SelectedDayEvents { get; }
    public ObservableCollection<CalendarLegendMember> LegendMembers { get; }

    public bool IsLoading
    {
        get => isLoading;
        set
        {
            if (SetProperty(ref isLoading, value))
            {
                NotifyPropertyChanged(nameof(ShowSkeleton));
                NotifyPropertyChanged(nameof(ShowMonth));
                NotifyPropertyChanged(nameof(ShowWeek));
                NotifyPropertyChanged(nameof(ShowDayCard));
            }
        }
    }

    public bool ShowSkeleton => isLoading && !hasLoaded;
    public bool ShowMonth => !ShowSkeleton && IsMonthMode;
    public bool ShowWeek => !ShowSkeleton && IsWeekMode;
    public bool ShowDayCard => !ShowSkeleton && IsMonthMode;

    public string Mode
    {
        get => mode;
        private set
        {
            if (SetProperty(ref mode, value))
            {
                Preferences.Default.Set(ModePrefKey, value);
                NotifyPropertyChanged(nameof(IsMonthMode));
                NotifyPropertyChanged(nameof(IsWeekMode));
                NotifyPropertyChanged(nameof(ShowMonth));
                NotifyPropertyChanged(nameof(ShowWeek));
                NotifyPropertyChanged(nameof(ShowDayCard));
            }
        }
    }

    public bool IsMonthMode => mode == "month";
    public bool IsWeekMode => mode == "week";

    public string MonthLabel => monthAnchor.ToString("MMMM yyyy");

    public DateTime SelectedDate
    {
        get => selectedDate;
        private set
        {
            if (SetProperty(ref selectedDate, value))
            {
                NotifyPropertyChanged(nameof(SelectedDayLabel));
                NotifyPropertyChanged(nameof(SelectedDayCount));
                RebuildSelectedDayEvents();
            }
        }
    }

    public string SelectedDayLabel => selectedDate.ToString("dddd, MMM d");

    public string SelectedDayCount
    {
        get
        {
            var n = SelectedDayEvents.Count;
            return $"{n} {(n == 1 ? "event" : "events")}";
        }
    }

    public SyncState SyncState
    {
        get => syncState;
        private set
        {
            if (SetProperty(ref syncState, value))
            {
                NotifyPropertyChanged(nameof(SyncDotColor));
                NotifyPropertyChanged(nameof(SyncLabel));
            }
        }
    }

    public string SyncDotColor => syncState.DotColor;
    public string SyncLabel => syncState.Label;

    public ICommand ToggleModeCommand { get; }
    public ICommand SelectDayCommand { get; }
    public ICommand PrevMonthCommand { get; }
    public ICommand NextMonthCommand { get; }

    public Task OnTabSelectedAsync(string? tabKey) => HandleTabSelectionAsync(tabKey);

    public async Task InitializeAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        var sw = Stopwatch.StartNew();
        try
        {
            if (memberCache != null)
            {
                try
                {
                    var members = await memberCache.GetMembersAsync();
                    memberNameById = members.ToDictionary(m => m.Id, m => m.Name, StringComparer.OrdinalIgnoreCase);

                    LegendMembers.Clear();
                    foreach (var m in members)
                        LegendMembers.Add(new CalendarLegendMember { MemberId = m.Id, DisplayName = m.Name });
                    LegendMembers.Add(new CalendarLegendMember { MemberId = "home", DisplayName = "Shared" });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Calendar] MemberCache error: {ex.Message}");
                }
            }

            var fetched = await calendarService.GetUpcomingEventsAsync();
            events.Clear();
            events.AddRange(fetched);

            Events.Clear();
            foreach (var e in events.OrderBy(e => e.Start)) Events.Add(e);

            BuildMonthCells();
            BuildWeek();
            RebuildSelectedDayEvents();
            SyncState = new SyncState { LastSyncAt = DateTime.Now.AddMinutes(-2) };
            NotifyPropertyChanged(nameof(MonthLabel));
            hasLoaded = true;
            NotifyPropertyChanged(nameof(ShowSkeleton));
            NotifyPropertyChanged(nameof(ShowMonth));
            NotifyPropertyChanged(nameof(ShowWeek));
            NotifyPropertyChanged(nameof(ShowDayCard));

            // Minimum skeleton display so a full left→right shimmer pass is visible
            // even when the data source returns near-instantly.
            var remaining = 1200 - (int)sw.ElapsedMilliseconds;
            if (remaining > 0) await Task.Delay(remaining);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Calendar] Initialize failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetMode(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        Mode = key.ToLowerInvariant() == "month" ? "month" : "week";
    }

    private void SelectDay(MonthCell? cell)
    {
        if (cell?.Date == null) return;
        SelectedDate = cell.Date.Value;
        foreach (var c in MonthCells) c.IsSelected = c.Date == cell.Date;
        // Re-emit cells so the UI reflects selection changes (mutating in-place doesn't notify).
        var snapshot = MonthCells.ToList();
        MonthCells.Clear();
        foreach (var c in snapshot) MonthCells.Add(c);
    }

    private void ShiftMonth(int delta)
    {
        monthAnchor = monthAnchor.AddMonths(delta);
        BuildMonthCells();
        NotifyPropertyChanged(nameof(MonthLabel));
    }

    private void BuildMonthCells()
    {
        MonthCells.Clear();
        var first = new DateTime(monthAnchor.Year, monthAnchor.Month, 1);
        var firstDow = ((int)first.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var daysInMonth = DateTime.DaysInMonth(first.Year, first.Month);
        var today = DateTime.Today;

        for (var i = 0; i < firstDow; i++)
            MonthCells.Add(new MonthCell { Day = null });

        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(first.Year, first.Month, d);
            var dayEvents = events.Where(e => e.Start.Date == date).ToList();
            MonthCells.Add(new MonthCell
            {
                Day = d,
                Date = date,
                IsToday = date == today,
                IsSelected = date == SelectedDate.Date,
                MemberDots = dayEvents.Take(3)
                    .Select(e => memberNameById.TryGetValue(e.Member ?? string.Empty, out var name) ? name : e.Member ?? string.Empty)
                    .ToList(),
                OverflowCount = Math.Max(0, dayEvents.Count - 3)
            });
        }

        while (MonthCells.Count % 7 != 0)
            MonthCells.Add(new MonthCell { Day = null });
    }

    private void BuildWeek()
    {
        WeekDays.Clear();
        WeekBlocks.Clear();

        var today = DateTime.Today;
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = today.AddDays(-diff);
        var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        for (var i = 0; i < 7; i++)
        {
            var d = monday.AddDays(i);
            WeekDays.Add(new WeekDayHeader
            {
                Label = labels[i],
                DayNumber = d.Day,
                IsToday = d.Date == today,
                Date = d
            });
        }

        var weekEnd = monday.AddDays(7);
        foreach (var e in events.Where(e => e.Start >= monday && e.Start < weekEnd))
        {
            var dayIndex = (int)(e.Start.Date - monday).TotalDays;
            var startHour = e.Start.Hour + e.Start.Minute / 60.0;
            var dur = e.End.HasValue ? Math.Max(0.25, (e.End.Value - e.Start).TotalHours) : 1.0;
            WeekBlocks.Add(new WeekEventBlock
            {
                Id = e.Id,
                DayIndex = dayIndex,
                StartHour = startHour,
                DurationHours = dur,
                Title = e.Title,
                Member = e.Member
            });
        }
    }

    private void RebuildSelectedDayEvents()
    {
        SelectedDayEvents.Clear();
        foreach (var e in events.Where(e => e.Start.Date == selectedDate.Date).OrderBy(e => e.Start))
            SelectedDayEvents.Add(e);
        NotifyPropertyChanged(nameof(SelectedDayCount));
    }
}
