using System.Windows.Input;
using LotteryDetectionMobile.Models.Voice;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class TaskPreviewViewModel : BaseViewModel, IQueryAttributable
{
    public static readonly string[] CategoryOptions = { "Home", "Errands", "Health", "School", "Work" };

    private readonly INavigationService navigationService;
    private string category = "Home";
    private bool isSaving;
    private string owner = string.Empty;
    private string priority = "Med";
    private string? suggestionText;
    private string title = string.Empty;
    private DateTime when = DateTime.Now.AddHours(2);

    public TaskPreviewViewModel()
        : this(NavigationService.Default)
    {
    }

    public TaskPreviewViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave);
        NewRecordingCommand = new Command(async () => await navigationService.NavigateBackAsync());
        CyclePriorityCommand = new Command(CyclePriority);
        ApplySuggestionCommand = new Command(ApplySuggestion);
    }

    public string Title
    {
        get => title;
        set
        {
            if (SetProperty(ref title, value))
            {
                NotifyPropertyChanged(nameof(CanSave));
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }
    }

    public string Owner
    {
        get => owner;
        set => SetProperty(ref owner, value);
    }

    public DateTime When
    {
        get => when;
        set => SetProperty(ref when, value);
    }

    public string Priority
    {
        get => priority;
        set => SetProperty(ref priority, value);
    }

    public string Category
    {
        get => category;
        set => SetProperty(ref category, value);
    }

    public string? SuggestionText
    {
        get => suggestionText;
        set
        {
            if (SetProperty(ref suggestionText, value))
                NotifyPropertyChanged(nameof(HasSuggestion));
        }
    }

    public bool HasSuggestion => !string.IsNullOrEmpty(SuggestionText);

    public bool IsSaving
    {
        get => isSaving;
        set
        {
            if (SetProperty(ref isSaving, value))
            {
                NotifyPropertyChanged(nameof(CanSave));
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }
    }

    public bool CanSave => !IsSaving && !string.IsNullOrWhiteSpace(Title);

    public ICommand SaveCommand { get; }
    public ICommand NewRecordingCommand { get; }
    public ICommand CyclePriorityCommand { get; }
    public ICommand ApplySuggestionCommand { get; }

    public event EventHandler<TaskDraft>? Saved;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(nameof(TaskDraft), out var raw) && raw is TaskDraft draft)
            ApplyDraft(draft);
    }

    public void ApplyDraft(TaskDraft draft)
    {
        Title = draft.Title;
        Owner = draft.Owner ?? string.Empty;
        When = draft.When;
        Priority = string.IsNullOrEmpty(draft.Priority) ? "Med" : draft.Priority;
        Category = string.IsNullOrEmpty(draft.Category) ? "Home" : draft.Category;
        SuggestionText = draft.SuggestionText;
    }

    public void SetOwner(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        Owner = value;
    }

    public void SetCategory(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        Category = value;
    }

    public void SetWhen(DateTime value)
    {
        When = value;
    }

    private void CyclePriority()
    {
        Priority = Priority switch
        {
            "Low" => "Med",
            "Med" => "High",
            _ => "Low"
        };
    }

    private void ApplySuggestion()
    {
        SuggestionText = null;
    }

    private async Task SaveAsync()
    {
        if (!CanSave) return;
        IsSaving = true;
        try
        {
            var draft = new TaskDraft
            {
                Title = Title.Trim(),
                Owner = Owner,
                When = When,
                Priority = Priority,
                Category = Category
            };
            Saved?.Invoke(this, draft);
            await navigationService.NavigateBackAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
