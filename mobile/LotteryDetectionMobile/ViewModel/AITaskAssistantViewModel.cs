using System.Collections.ObjectModel;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class AITaskAssistantViewModel : TabNavigationViewModel
{
    public static readonly string[] FilterKeys = { "all", "conflict", "create", "delegate" };

    private readonly IAIService aiService;
    private readonly List<AssistantSuggestion> source = new();

    private bool hasLoaded;
    private bool isBusy;
    private string activeFilter = "all";

    public AITaskAssistantViewModel()
        : this(NavigationService.Default, MockAIService.Instance)
    {
    }

    public AITaskAssistantViewModel(INavigationService navigationService, IAIService aiService)
        : base(navigationService)
    {
        this.aiService = aiService;
        Suggestions = new ObservableCollection<AssistantSuggestion>();
        QuickPrompts = new ObservableCollection<string>
        {
            "Plan dinners", "Reassign Thursday", "What's overdue?"
        };

        RefreshCommand = new Command(async () => await InitializeAsync(), () => !IsBusy);
        SetFilterCommand = new Command<string>(SetFilter);
        AcceptCommand = new Command<AssistantSuggestion>(Accept);
        DismissCommand = new Command<AssistantSuggestion>(Dismiss);
        AskCommand = new Command<string>(async prompt =>
            await navigationService.NavigateToChatToTaskAsync());
    }

    public ObservableCollection<AssistantSuggestion> Suggestions { get; }
    public ObservableCollection<string> QuickPrompts { get; }

    public new bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetProperty(ref isBusy, value))
            {
                ((Command)RefreshCommand).ChangeCanExecute();
                NotifyPropertyChanged(nameof(ShowSkeleton));
            }
        }
    }

    public bool ShowSkeleton => IsBusy && !hasLoaded;

    public string ActiveFilter
    {
        get => activeFilter;
        set
        {
            if (SetProperty(ref activeFilter, value))
            {
                Refilter();
                NotifyFilterFlags();
            }
        }
    }

    public bool IsFilterAll => activeFilter == "all";
    public bool IsFilterConflict => activeFilter == "conflict";
    public bool IsFilterCreate => activeFilter == "create";
    public bool IsFilterDelegate => activeFilter == "delegate";

    public int CountAll => source.Count;
    public int CountConflict => source.Count(s => s.Kind == "conflict");
    public int CountCreate => source.Count(s => s.Kind == "create");
    public int CountDelegate => source.Count(s => s.Kind == "delegate");

    public int VisibleCount => Suggestions.Count;
    public bool HasSuggestions => Suggestions.Count > 0;
    public bool IsEmpty => Suggestions.Count == 0;

    public string HeroEyebrow => "TODAY'S BRIEF";
    public string HeroTitle => $"{VisibleCount} suggestions to review";

    public string HeroSubtitle
    {
        get
        {
            var conflicts = source.Count(s => s.Kind == "conflict");
            var creates = source.Count(s => s.Kind == "create");
            if (source.Count == 0) return "All caught up — nothing needs review right now.";
            if (conflicts > 0 && creates > 0)
                return $"I noticed {(conflicts == 1 ? "a conflict" : $"{conflicts} conflicts")} and {creates} task{(creates == 1 ? "" : "s")} worth scheduling now.";
            if (conflicts > 0)
                return $"I noticed {(conflicts == 1 ? "a conflict" : $"{conflicts} conflicts")} that need attention.";
            return $"{creates} task{(creates == 1 ? "" : "s")} worth scheduling now.";
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand DismissCommand { get; }
    public ICommand AskCommand { get; }

    public async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        source.Clear();
        var items = await aiService.GetSuggestionsAsync();
        foreach (var item in items) source.Add(item);

        Refilter();
        NotifyAllCounts();
        hasLoaded = true;
        NotifyPropertyChanged(nameof(ShowSkeleton));
        IsBusy = false;
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    private void SetFilter(string? key)
    {
        if (string.IsNullOrEmpty(key)) return;
        ActiveFilter = key;
    }

    private void Accept(AssistantSuggestion? item)
    {
        if (item == null) return;
        RemoveFromSource(item);
    }

    private void Dismiss(AssistantSuggestion? item)
    {
        if (item == null) return;
        RemoveFromSource(item);
    }

    private void RemoveFromSource(AssistantSuggestion item)
    {
        source.RemoveAll(s => s.Id == item.Id);
        Refilter();
        NotifyAllCounts();
    }

    private void Refilter()
    {
        Suggestions.Clear();
        var filtered = activeFilter == "all"
            ? source
            : source.Where(s => s.Kind == activeFilter);
        foreach (var item in filtered) Suggestions.Add(item);

        NotifyPropertyChanged(nameof(VisibleCount));
        NotifyPropertyChanged(nameof(HasSuggestions));
        NotifyPropertyChanged(nameof(IsEmpty));
        NotifyPropertyChanged(nameof(HeroTitle));
        NotifyPropertyChanged(nameof(HeroSubtitle));
    }

    private void NotifyAllCounts()
    {
        NotifyPropertyChanged(nameof(CountAll));
        NotifyPropertyChanged(nameof(CountConflict));
        NotifyPropertyChanged(nameof(CountCreate));
        NotifyPropertyChanged(nameof(CountDelegate));
    }

    private void NotifyFilterFlags()
    {
        NotifyPropertyChanged(nameof(IsFilterAll));
        NotifyPropertyChanged(nameof(IsFilterConflict));
        NotifyPropertyChanged(nameof(IsFilterCreate));
        NotifyPropertyChanged(nameof(IsFilterDelegate));
    }
}
