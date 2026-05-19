using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Models.Voice;
using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Logging;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.ViewModel;

public class MyTasksViewModel : TabNavigationViewModel
{
    private static readonly TimeSpan DeleteTimeout = TimeSpan.FromSeconds(12);

    private readonly ITaskService taskService;
    private readonly List<VoiceTaskListItem> _allTasks = new();
    private bool _isEmpty;
    private bool _isDeletingTask;
    private bool _isLoading;
    private bool _isRefreshing;
    private bool _hasLoaded;
    private string _selectedFilter = "All";

    public MyTasksViewModel() : this(
        NavigationService.Default,
        MauiProgram.Services?.GetService<ITaskService>() ?? MockTaskService.Instance)
    {
    }

    public MyTasksViewModel(INavigationService navigationService, ITaskService taskService) : base(navigationService)
    {
        this.taskService = taskService;
        RefreshCommand = new Command(
            async () => await LoadTasksAsync(isRefresh: true),
            () => !_isLoading && !_isRefreshing);
        SelectTaskCommand = new Command<VoiceTaskListItem>(async task =>
        {
            if (task == null) return;
            TaskDetailViewModel.Seed(task); // optimistic-UI seed for instant detail render
            await NavigateToDetailAsync(task.TaskId.ToString(), editMode: false);
        });
        SetFilterCommand = new Command<string>(filter => SelectedFilter = filter);
        EditTaskCommand = new Command<VoiceTaskListItem>(async task =>
        {
            if (task == null) return;
            TaskDetailViewModel.Seed(task);
            await NavigateToDetailAsync(task.TaskId.ToString(), editMode: true);
        });
        DeleteTaskCommand = new Command<VoiceTaskListItem>(async task => await ConfirmAndDeleteAsync(task));
    }

    public ObservableCollection<VoiceTaskListItem> Tasks { get; } = new();
    public ObservableCollection<TaskGroup> GroupedTasks { get; } = new();
    public ObservableCollection<string> Filters { get; } = new() { "All", "Open", "Due Today", "Completed" };

    public bool IsDeletingTask
    {
        get => _isDeletingTask;
        set => SetProperty(ref _isDeletingTask, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                (RefreshCommand as Command)?.ChangeCanExecute();
                NotifyPropertyChanged(nameof(ShowSkeleton));
            }
        }
    }

    public bool ShowSkeleton => IsLoading && !_hasLoaded;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (SetProperty(ref _isRefreshing, value))
                (RefreshCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
                ApplyFilter();
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SelectTaskCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand EditTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }

    public async Task InitializeAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            await LoadTasksAsync(isRefresh: false);
        }
        finally
        {
            _hasLoaded = true;
            IsLoading = false;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
    }

    private async Task NavigateToDetailAsync(string taskId, bool editMode)
    {
        await navigationService.NavigateToTaskDetailAsync(taskId, editMode);
    }

    private async Task ConfirmAndDeleteAsync(VoiceTaskListItem? task)
    {
        if (task == null || IsDeletingTask) return;

        var confirmed = await AppDialog.ShowConfirmAsync(
            title: "Delete task?",
            message: string.IsNullOrWhiteSpace(task.Title)
                ? "This cannot be undone."
                : $"{task.Title}\nThis cannot be undone.",
            acceptText: "Delete",
            cancelText: "Cancel",
            danger: true,
            icon: "🗑",
            iconBackground: "#FEE2E2");
        if (!confirmed) return;

        IsDeletingTask = true;
        var success = false;
        try
        {
            RemoteLogService.Instance.Info("MyTasks", $"Deleting mock task {task.TaskId}");
            success = await WithTimeoutAsync(taskService.DeleteTaskAsync(task.TaskId.ToString()), DeleteTimeout);

            if (success)
            {
                _allTasks.RemoveAll(t => t.TaskId == task.TaskId);
                ApplyFilter();
                RemoteLogService.Instance.Info("MyTasks", $"Deleted task {task.TaskId}");
            }
            else
            {
                RemoteLogService.Instance.Warn("MyTasks", $"Delete failed for task {task.TaskId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MyTasks] ExecuteDeleteAsync error: {ex.Message}");
            RemoteLogService.Instance.Error("MyTasks", $"Delete error for task {task.TaskId}", ex);
        }
        finally
        {
            IsDeletingTask = false;
        }

        if (!success)
            await AppDialog.ShowAlertAsync("Delete failed", "Could not delete this task. Please try again.");
    }

    private static async Task<bool> WithTimeoutAsync(Task<bool> operation, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(operation, Task.Delay(timeout));
        if (completed != operation) return false;
        return await operation;
    }

    private async Task LoadTasksAsync(bool isRefresh = true)
    {
        if (isRefresh) IsRefreshing = true;
        try
        {
            var result = (await taskService.GetBoardTasksAsync()).Select(MapToVoiceTask).ToList();

            Debug.WriteLine($"[MyTasks] Loaded {result.Count} mock tasks");

            _allTasks.Clear();
            _allTasks.AddRange(result);
            ApplyFilter();
            _hasLoaded = true;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MyTasks] LoadTasksAsync error: {ex.Message}");
            _allTasks.Clear();
            ApplyFilter();
            _hasLoaded = true;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
        finally
        {
            if (isRefresh) IsRefreshing = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _selectedFilter switch
        {
            "Open" => _allTasks.Where(t => !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            "Due Today" => _allTasks.Where(t => IsDueToday(t.DueDateTime)),
            "Completed" => _allTasks.Where(t =>
                string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            _ => _allTasks.AsEnumerable()
        };

        var items = filtered.ToList();

        Tasks.Clear();
        foreach (var item in items)
            Tasks.Add(item);

        GroupedTasks.Clear();
        foreach (var group in GroupByDate(items))
            GroupedTasks.Add(group);

        IsEmpty = Tasks.Count == 0;
    }

    private static bool IsCompleted(VoiceTaskListItem t)
    {
        return string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<TaskGroup> GroupByDate(List<VoiceTaskListItem> tasks)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var endOfWeek = today.AddDays(7 - (int)today.DayOfWeek);

        // Active tasks grouped by due date, then completed tasks at the end
        var groups = new (string Name, Func<VoiceTaskListItem, bool> Predicate)[]
        {
            ("Overdue", t => !IsCompleted(t) && TryParseDate(t.DueDateTime, out var d) && d < now),
            ("Today", t => !IsCompleted(t) && TryParseDate(t.DueDateTime, out var d) && d.Date == today),
            ("Tomorrow", t => !IsCompleted(t) && TryParseDate(t.DueDateTime, out var d) && d.Date == tomorrow),
            ("This Week",
                t => !IsCompleted(t) && TryParseDate(t.DueDateTime, out var d) && d.Date > tomorrow &&
                     d.Date <= endOfWeek),
            ("Later", t => !IsCompleted(t) && TryParseDate(t.DueDateTime, out var d) && d.Date > endOfWeek),
            ("No Date", t => !IsCompleted(t) && !TryParseDate(t.DueDateTime, out _)),
            ("Completed", t => IsCompleted(t))
        };

        foreach (var (name, predicate) in groups)
        {
            var items = tasks.Where(predicate).ToList();
            if (items.Count > 0)
                yield return new TaskGroup(name, items);
        }
    }

    private static bool TryParseDate(string? raw, out DateTime date)
    {
        date = default;
        if (string.IsNullOrEmpty(raw)) return false;
        // Handle date range format: take start date
        var value = raw.Contains('/') ? raw.Split('/')[0] : raw;
        return DateTime.TryParse(value.Trim(), out date);
    }

    private static bool IsDueToday(string? raw)
    {
        return TryParseDate(raw, out var date) && date.Date == DateTime.Now.Date;
    }

    private static VoiceTaskListItem MapToVoiceTask(TaskItem item)
    {
        var taskId = Guid.TryParse(item.Id, out var parsed) ? parsed : StableGuid(item.Id);
        return new VoiceTaskListItem
        {
            TaskId = taskId,
            Title = item.Title,
            Status = item.Status,
            Assignee = item.Owner,
            Priority = item.Priority,
            DueDateTime = item.DueDate?.ToString("O") ?? string.Empty,
            Category = item.Category,
            CreatedAt = item.UpdatedAt ?? item.DueDate ?? DateTime.Now,
            TranscriptPreview = item.Description
        };
    }

    private static Guid StableGuid(string? value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value ?? Guid.NewGuid().ToString()));
        return new Guid(hash[..16]);
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }
}
