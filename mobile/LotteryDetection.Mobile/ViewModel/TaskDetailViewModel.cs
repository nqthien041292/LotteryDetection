using System.Diagnostics;
using System.Windows.Input;
using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Models.Voice;
using LotteryDetection.Mobile.Services.Dialogs;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.ViewModel;

public class TaskDetailViewModel : TabNavigationViewModel
{
    private readonly ITaskService taskService;
    private readonly IFamilyMemberCache? memberCache;
    // Optimistic-UI seed cache: callers can deposit a partial preview built from list data
    // so this page renders fields instantly while the full API call runs in the background.
    private static readonly Dictionary<Guid, VoiceTaskPreview> SeedCache = new();

    /// <summary>Deposit a partial preview from a list item before navigating to the detail page.</summary>
    public static void Seed(VoiceTaskListItem item)
    {
        if (item == null || item.TaskId == Guid.Empty) return;
        SeedCache[item.TaskId] = new VoiceTaskPreview
        {
            TaskId = item.TaskId,
            Status = item.Status,
            Title = item.Title,
            Assignee = item.Assignee,
            Priority = item.Priority,
            DueDateTime = item.DueDateTime,
            Category = item.Category,
            Transcript = item.TranscriptPreview ?? string.Empty
        };
    }

    private string _editAssignee = string.Empty;
    private string _assigneeMemberId = string.Empty;
    private string _editCategory = string.Empty;
    private string _editDueDateTime = string.Empty;
    private string _editLocation = string.Empty;
    private string _editNotes = string.Empty;
    private string _editPriority = string.Empty;

    // Editable field backing stores
    private string _editTitle = string.Empty;
    private string? _errorMessage;
    private bool _isCompleted;
    private bool _isCompleting;

    // Edit mode state
    private bool _isEditing;
    // Default true so the page renders the skeleton from the moment its
    // BindingContext is created — before OnAppearing → InitializeAsync runs.
    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isDeleting;
    private bool _isAddingToBoard;
    private VoiceTaskPreview? _task;
    private string _taskId = string.Empty;

    public TaskDetailViewModel() : this(
        NavigationService.Default,
        MauiProgram.Services?.GetService<ITaskService>() ?? MockTaskService.Instance,
        MauiProgram.Services?.GetService<IFamilyMemberCache>())
    {
    }

    public TaskDetailViewModel(INavigationService navigationService, ITaskService taskService,
        IFamilyMemberCache? memberCache = null) : base(navigationService)
    {
        this.taskService = taskService;
        this.memberCache = memberCache;
        CompleteTaskCommand = new Command(async () => await CompleteTaskAsync(), () => !IsCompleting && !IsCompleted);
        EditCommand = new Command(() => EnterEditMode());
        SaveCommand = new Command(async () => await SaveChangesAsync());
        CancelEditCommand = new Command(() => CancelEdit());
        DeleteCommand = new Command(async () => await DeleteTaskAsync(), () => !IsDeleting);
    }

    public VoiceTaskPreview? Task
    {
        get => _task;
        set
        {
            if (SetProperty(ref _task, value))
            {
                NotifyPropertyChanged(nameof(AssigneeInitial));
                NotifyPropertyChanged(nameof(ShowSkeleton));
            }
        }
    }

    public string TaskId
    {
        get => _taskId;
        set => SetProperty(ref _taskId, value);
    }

    public bool StartInEditMode { get; set; }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
                NotifyPropertyChanged(nameof(ShowSkeleton));
        }
    }

    public bool ShowSkeleton => IsLoading;

    public bool IsCompleting
    {
        get => _isCompleting;
        set
        {
            if (SetProperty(ref _isCompleting, value))
                ((Command)CompleteTaskCommand).ChangeCanExecute();
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
                ((Command)CompleteTaskCommand).ChangeCanExecute();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsDeleting
    {
        get => _isDeleting;
        set
        {
            if (SetProperty(ref _isDeleting, value))
                ((Command)DeleteCommand).ChangeCanExecute();
        }
    }

    public bool IsAddingToBoard
    {
        get => _isAddingToBoard;
        set => SetProperty(ref _isAddingToBoard, value);
    }

    public string EditTitle
    {
        get => _editTitle;
        set => SetProperty(ref _editTitle, value);
    }

    public string EditAssignee
    {
        get => _editAssignee;
        set => SetProperty(ref _editAssignee, value);
    }

    public string AssigneeMemberId
    {
        get => _assigneeMemberId;
        private set
        {
            if (SetProperty(ref _assigneeMemberId, value))
                NotifyPropertyChanged(nameof(AssigneeInitial));
        }
    }

    public string AssigneeInitial
    {
        get
        {
            var name = Task?.Assignee;
            return string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[0].ToString().ToUpperInvariant();
        }
    }

    public string EditDueDateTime
    {
        get => _editDueDateTime;
        set => SetProperty(ref _editDueDateTime, value);
    }

    public string EditPriority
    {
        get => _editPriority;
        set => SetProperty(ref _editPriority, value);
    }

    public string EditCategory
    {
        get => _editCategory;
        set => SetProperty(ref _editCategory, value);
    }

    public string EditLocation
    {
        get => _editLocation;
        set => SetProperty(ref _editLocation, value);
    }

    public string EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    public bool HasAiSuggestion { get; private set; }
    public string AiSuggestionTitle { get; private set; } = string.Empty;
    public string AiSuggestionDescription { get; private set; } = string.Empty;

    public ICommand CompleteTaskCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteCommand { get; }

    public async Task InitializeAsync(string? id = null)
    {
        if (!string.IsNullOrWhiteSpace(id)) TaskId = id;
        if (string.IsNullOrWhiteSpace(TaskId))
        {
            IsLoading = false;
            return;
        }
        if (!Guid.TryParse(TaskId, out var taskGuid))
        {
            IsLoading = false;
            return;
        }

        ErrorMessage = null;
        // Always show the skeleton during the initial load. The seed (if present)
        // hydrates Task in the background so the page snaps to fully-populated
        // content when the API finishes — no flash of half-empty fields.
        IsLoading = true;

        var hasSeed = SeedCache.TryGetValue(taskGuid, out var seed);
        if (hasSeed && seed != null)
        {
            Task = seed;
            IsCompleted = string.Equals(seed.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            var mobileTask = await taskService.GetTaskByIdAsync(TaskId);
            var preview = mobileTask != null
                ? MapToPreview(mobileTask)
                : await LoadVoicePreviewAsync(taskGuid);

            if (preview != null)
            {
                Task = preview;
                IsCompleted = string.Equals(preview.Status, "Completed", StringComparison.OrdinalIgnoreCase);
                SeedCache[taskGuid] = preview; // refresh cache for next visit
            }

            // Resolve assignee member ID for avatar color
            if (!string.IsNullOrEmpty(mobileTask?.AssigneeId))
            {
                AssigneeMemberId = mobileTask.AssigneeId;
            }
            else if (memberCache != null && !string.IsNullOrEmpty(preview?.Assignee))
            {
                try
                {
                    var members = await memberCache.GetMembersAsync();
                    var match = members.FirstOrDefault(m =>
                        string.Equals(m.Name, preview.Assignee, StringComparison.OrdinalIgnoreCase));
                    if (match != null) AssigneeMemberId = match.Id;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TaskDetail] MemberCache lookup error: {ex.Message}");
                }
            }
            else if (!hasSeed)
            {
                ErrorMessage = "Task not found";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskDetail] Load error: {ex.Message}");
            if (!hasSeed) ErrorMessage = "Failed to load task details";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void EnterEditMode()
    {
        if (Task == null) return;
        EditTitle = Task.Title ?? string.Empty;
        EditAssignee = Task.Assignee ?? string.Empty;
        EditDueDateTime = Task.DueDateTime ?? string.Empty;
        EditPriority = Task.Priority ?? string.Empty;
        EditCategory = Task.Category ?? string.Empty;
        EditLocation = Task.Location ?? string.Empty;
        EditNotes = Task.Notes ?? string.Empty;
        IsEditing = true;
    }

    private void CancelEdit()
    {
        IsEditing = false;
        ErrorMessage = null;
    }

    public async Task SaveChangesAsync()
    {
        if (!IsEditing || string.IsNullOrWhiteSpace(TaskId) || IsSaving) return;

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var updated = await taskService.UpdateTaskContentAsync(new TaskItem
            {
                Id = TaskId,
                Title = EditTitle,
                Owner = EditAssignee,
                AssigneeId = EditAssignee,
                DueDate = ParseDate(EditDueDateTime),
                Priority = EditPriority,
                Category = EditCategory,
                Description = EditNotes
            });
            if (updated != null)
            {
                Task = MapToPreview(updated);
                IsCompleted = string.Equals(Task.Status, "Completed", StringComparison.OrdinalIgnoreCase);
                IsEditing = false;
            }
            else
            {
                ErrorMessage = "Failed to save changes";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskDetail] Save error: {ex.Message}");
            ErrorMessage = "Failed to save changes";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task CompleteTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(TaskId) || IsCompleting || IsCompleted) return;

        IsCompleting = true;
        ErrorMessage = null;

        try
        {
            var updated = await taskService.UpdateTaskStatusAsync(TaskId, "Completed");

            if (updated != null)
            {
                IsCompleted = true;
                Task = MapToPreview(updated);
                NotifyPropertyChanged(nameof(Task));
            }
            else
            {
                ErrorMessage = "Failed to mark task as completed";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskDetail] Complete error: {ex.Message}");
            ErrorMessage = "Failed to mark task as completed";
        }
        finally
        {
            IsCompleting = false;
        }
    }

    /// <summary>
    ///     Promotes the voice task into a FamilyTask (the source backing the Dashboard "board"),
    ///     then navigates to the Dashboard so the user sees it there.
    ///     If the task has already been promoted (status TaskCreated/Completed), just navigates.
    /// </summary>
    public async Task AddToBoardAsync()
    {
        if (string.IsNullOrWhiteSpace(TaskId)) return;
        if (!Guid.TryParse(TaskId, out var taskGuid)) return;
        if (IsAddingToBoard) return;

        IsAddingToBoard = true;
        ErrorMessage = null;

        try
        {
            var status = Task?.Status ?? string.Empty;
            var alreadyPromoted =
                string.Equals(status, "TaskCreated", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);

            if (!alreadyPromoted)
            {
                if (Task != null) Task.Status = "TaskCreated";
                NotifyPropertyChanged(nameof(Task));

                // Refresh seed so a re-entry shows the post-promote state immediately
                // instead of flashing the stale "Add to board" label from the cached preview.
                if (Task != null) SeedCache[taskGuid] = Task;
            }

            await navigationService.NavigateToDashboardAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskDetail] AddToBoard error: {ex.Message}");
            ErrorMessage = "Couldn't add task to board.";
        }
        finally
        {
            IsAddingToBoard = false;
        }
    }

    private async Task DeleteTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(TaskId) || IsDeleting) return;

        var confirm = await AppDialog.ShowConfirmAsync(
            title: "Delete Task",
            message: "Are you sure you want to delete this task? This cannot be undone.",
            acceptText: "Delete",
            cancelText: "Cancel",
            danger: true,
            icon: "🗑",
            iconBackground: "#FEE2E2");

        if (!confirm) return;

        IsDeleting = true;
        ErrorMessage = null;

        try
        {
            var success = await taskService.DeleteTaskAsync(TaskId);

            if (success)
                await navigationService.NavigateBackAsync();
            else
                ErrorMessage = "Failed to delete task";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskDetail] Delete error: {ex.Message}");
            ErrorMessage = "Failed to delete task";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    private static Task<VoiceTaskPreview?> LoadVoicePreviewAsync(Guid taskGuid)
    {
        return System.Threading.Tasks.Task.FromResult<VoiceTaskPreview?>(new VoiceTaskPreview
        {
            TaskId = taskGuid,
            Title = "Mock lottery ticket review",
            Assignee = "Lottery Demo",
            DueDateTime = DateTime.Today.ToString("O"),
            Priority = "Medium",
            Category = "Lottery",
            Notes = "Mock detail generated locally while backend APIs are pending.",
            Status = "Open"
        });
    }

    private static VoiceTaskPreview MapToPreview(TaskItem item)
    {
        return new VoiceTaskPreview
        {
            TaskId = Guid.TryParse(item.Id, out var id) ? id : Guid.Empty,
            Title = item.Title,
            Assignee = item.Owner,
            DueDateTime = item.DueDate?.ToString("O"),
            Priority = item.Priority,
            Category = item.Category,
            Notes = item.Description,
            Status = item.Status
        };
    }

    private static DateTime? ParseDate(string raw)
    {
        return DateTime.TryParse(raw, out var value) ? value : null;
    }
}
