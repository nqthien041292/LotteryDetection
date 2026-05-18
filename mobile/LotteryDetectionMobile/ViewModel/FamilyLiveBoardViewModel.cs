using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Board;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class FamilyLiveBoardViewModel : TabNavigationViewModel
{
    private readonly ITaskService taskService;
    private readonly IFamilyMemberCache? memberCache;
    private bool isRefreshing;
    private bool hasLoaded;
    private int selectedColumnIndex;

    public FamilyLiveBoardViewModel()
        : this(NavigationService.Default, MockTaskService.Instance)
    {
    }

    public FamilyLiveBoardViewModel(INavigationService navigationService, ITaskService taskService,
        IFamilyMemberCache? memberCache = null)
        : base(navigationService)
    {
        this.taskService = taskService;
        this.memberCache = memberCache;

        Tasks = new ObservableCollection<TaskItem>();
        Activity = new ObservableCollection<TaskItem>();
        Columns = new ObservableCollection<KanbanColumn>
        {
            new() { Id = "todo",  Label = "To do",       DotColor = "#94A3BE" },
            new() { Id = "doing", Label = "In progress", DotColor = "#1E5BFF" },
            new() { Id = "done",  Label = "Done",        DotColor = "#22C55E" },
        };
        foreach (var col in Columns)
            col.Cards.CollectionChanged += (_, _) => NotifyTabLabels();
        Presence = new ObservableCollection<PresenceMember>();

        RefreshCommand = new Command(async () => await LoadBoardAsync());
        SelectTaskCommand = new Command<TaskItem>(async t => await NavigateToTaskAsync(t));
        SelectCardCommand = new Command<BoardCard>(SelectCard);
        SelectColumnCommand = new Command<string>(SelectColumn);
        MoveCardCommand = new Command<(BoardCard card, string targetId)>(async tuple => await MoveCardAsync(tuple));

        // start in 'doing' to mirror bundle default
        selectedColumnIndex = 1;
    }

    public ObservableCollection<TaskItem> Tasks { get; }
    public ObservableCollection<TaskItem> Activity { get; }
    public ObservableCollection<KanbanColumn> Columns { get; }
    public ObservableCollection<PresenceMember> Presence { get; }

    public bool IsRefreshing
    {
        get => isRefreshing;
        set
        {
            if (SetProperty(ref isRefreshing, value))
                NotifyPropertyChanged(nameof(ShowSkeleton));
        }
    }

    public bool ShowSkeleton => IsRefreshing && !hasLoaded;

    public int SelectedColumnIndex
    {
        get => selectedColumnIndex;
        set
        {
            if (SetProperty(ref selectedColumnIndex, value))
            {
                NotifyPropertyChanged(nameof(IsTodoActive));
                NotifyPropertyChanged(nameof(IsDoingActive));
                NotifyPropertyChanged(nameof(IsDoneActive));
            }
        }
    }

    public bool IsTodoActive => selectedColumnIndex == 0;
    public bool IsDoingActive => selectedColumnIndex == 1;
    public bool IsDoneActive => selectedColumnIndex == 2;

    public string TodoTabLabel => FormatTabLabel(0);
    public string DoingTabLabel => FormatTabLabel(1);
    public string DoneTabLabel => FormatTabLabel(2);

    private string FormatTabLabel(int index)
    {
        if (index < 0 || index >= Columns.Count) return string.Empty;
        var col = Columns[index];
        return $"● {col.Label}  {col.Cards.Count}";
    }

    private void NotifyTabLabels()
    {
        NotifyPropertyChanged(nameof(TodoTabLabel));
        NotifyPropertyChanged(nameof(DoingTabLabel));
        NotifyPropertyChanged(nameof(DoneTabLabel));
    }

    public string PresenceLabel
    {
        get
        {
            var n = Presence.Count(p => p.IsOnline);
            return $"Live · {n} {(n == 1 ? "member" : "members")} online";
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SelectTaskCommand { get; }
    public ICommand SelectCardCommand { get; }
    public ICommand SelectColumnCommand { get; }
    public ICommand MoveCardCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadBoardAsync();
    }

    public Task OnTabSelectedAsync(string? tabKey) => HandleTabSelectionAsync(tabKey);

    public void MoveCardToColumn(BoardCard card, string targetId)
    {
        if (card == null || string.IsNullOrEmpty(targetId)) return;
        var src = Columns.FirstOrDefault(c => c.Id == card.Status);
        var dst = Columns.FirstOrDefault(c => c.Id == targetId);
        if (src == null || dst == null || src == dst) return;
        src.Cards.Remove(card);
        card.Status = targetId;
        dst.Cards.Add(card);
    }

    private async Task LoadBoardAsync()
    {
        if (IsRefreshing) return;
        IsRefreshing = true;
        try
        {
            Tasks.Clear();
            var items = (await taskService.GetBoardTasksAsync()).ToList();
            foreach (var t in items.OrderByDescending(t => t.IsPinned)) Tasks.Add(t);

            Activity.Clear();
            var activity = await taskService.GetBoardActivityAsync();
            foreach (var item in activity) Activity.Add(item);

            BuildColumns(items);
            await BuildPresenceAsync(items);
            hasLoaded = true;
            NotifyPropertyChanged(nameof(PresenceLabel));
            NotifyTabLabels();
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Board] Load failed: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void BuildColumns(IEnumerable<TaskItem> items)
    {
        foreach (var col in Columns) col.Cards.Clear();

        foreach (var t in items)
        {
            var status = MapStatus(t.Status);
            var col = Columns.FirstOrDefault(c => c.Id == status) ?? Columns[0];
            col.Cards.Add(MapCard(t, status));
        }

    }

    private static string MapStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return "todo";
        return status.ToLowerInvariant() switch
        {
            "completed" or "done" => "done",
            "inprogress" or "in_progress" or "doing" or "active" => "doing",
            _ => "todo"
        };
    }

    private static BoardCard MapCard(TaskItem t, string status)
    {
        var member = !string.IsNullOrEmpty(t.AssigneeId) ? t.AssigneeId : "home";

        var when = "Today";
        if (t.DueDate.HasValue)
        {
            var d = t.DueDate.Value;
            when = d.Date == DateTime.Today ? $"Today · {d:h:mm tt}"
                : d.Date == DateTime.Today.AddDays(1) ? $"Tomorrow · {d:h:mm tt}"
                : $"{d:ddd} · {d:h:mm tt}";
        }

        var priority = (t.Priority ?? "medium").ToLowerInvariant() switch
        {
            "high" => "high",
            "low" => "low",
            _ => "med"
        };

        return new BoardCard
        {
            Id = t.Id,
            Title = t.Title,
            Member = member,
            OwnerLabel = string.IsNullOrEmpty(t.Owner) ? "Shared" :
                char.ToUpperInvariant(t.Owner[0]) + t.Owner[1..],
            When = when,
            Priority = priority,
            IsLive = status == "doing" && t.IsPinned,
            Status = status
        };
    }

    private async Task BuildPresenceAsync(IEnumerable<TaskItem> items)
    {
        Dictionary<string, FamilyMember>? memberById = null;
        if (memberCache != null)
        {
            try
            {
                var cached = await memberCache.GetMembersAsync();
                memberById = cached.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Board] MemberCache error: {ex.Message}");
            }
        }

        Presence.Clear();
        var presenceItems = items
            .Where(t => !string.IsNullOrWhiteSpace(t.AssigneeId) || !string.IsNullOrWhiteSpace(t.Owner))
            .Select(t => string.IsNullOrWhiteSpace(t.AssigneeId) ? t.Owner : t.AssigneeId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5);

        foreach (var assigneeId in presenceItems)
        {
            var name = assigneeId;
            if (memberById != null && memberById.TryGetValue(assigneeId, out var m))
                name = m.Name;

            Presence.Add(new PresenceMember
            {
                Member = assigneeId,
                Initial = string.IsNullOrWhiteSpace(name) ? "?" : name[..1].ToUpperInvariant(),
                IsOnline = false
            });
        }
    }

    private void SelectCard(BoardCard? card)
    {
        if (card == null) return;
        _ = navigationService.NavigateToTaskDetailAsync(card.Id);
    }

    private void SelectColumn(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var index = Columns.ToList().FindIndex(c => c.Id == id);
        if (index >= 0) SelectedColumnIndex = index;
    }

    public async Task MoveCardAsync(BoardCard card, string targetId)
    {
        var previousStatus = card.Status;
        MoveCardToColumn(card, targetId);
        try
        {
            var updated = await taskService.MoveTaskAsync(card.Id, targetId);
            if (updated == null && previousStatus != targetId)
                MoveCardToColumn(card, previousStatus);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Board] Move failed: {ex.Message}");
            if (previousStatus != targetId)
                MoveCardToColumn(card, previousStatus);
        }
    }

    private Task MoveCardAsync((BoardCard card, string targetId) tuple)
    {
        return MoveCardAsync(tuple.card, tuple.targetId);
    }

    private Task NavigateToTaskAsync(TaskItem? task)
    {
        if (task == null) return Task.CompletedTask;
        return navigationService.NavigateToTaskDetailAsync(task.Id);
    }
}
