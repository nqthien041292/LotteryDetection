using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.ViewModel;

public class ChatToTaskViewModel : TabNavigationViewModel
{
    public static readonly string[] PriorityOptions = { "Low", "Med", "High" };

    private readonly IAIService aiService;
    private bool initialized;
    private bool isBusy;
    private bool isExtractedExpanded = true;
    private string makeTaskOwner = string.Empty;
    private string makeTaskPriority = "Med";
    private string makeTaskTitle = string.Empty;
    private DateTime makeTaskWhen = DateTime.Now.AddHours(2);
    private string messageText = string.Empty;
    private bool showMakeTaskSheet;

    public ChatToTaskViewModel()
        : this(
            NavigationService.Default,
            MauiProgram.Services?.GetService<IAIService>() ?? MockAIService.Instance)
    {
    }

    public ChatToTaskViewModel(INavigationService navigationService, IAIService aiService)
        : base(navigationService)
    {
        this.aiService = aiService;
        Messages = new ObservableCollection<ChatMessage>();
        GeneratedTasks = new ObservableCollection<TaskItem>();
        SendCommand = new Command(async () => await SendAsync(), () => !IsBusy);
        GenerateTasksCommand = new Command(async () => await GenerateTasksAsync(), () => !IsBusy);

        MakeTaskFromBubbleCommand = new Command<ChatMessage>(OpenMakeTaskSheet);
        DismissMakeTaskCommand = new Command(() => ShowMakeTaskSheet = false);
        ConfirmMakeTaskCommand = new Command(async () => await ConfirmMakeTaskAsync(), () => CanConfirmMakeTask);
        ConfirmAllTasksCommand = new Command(async () => await ConfirmAllTasksAsync(), () => !IsBusy && GeneratedTasks.Count > 0);
        CycleMakeTaskPriorityCommand = new Command(CyclePriority);
        ToggleExtractedExpandedCommand = new Command(() => IsExtractedExpanded = !IsExtractedExpanded);
    }

    public ObservableCollection<ChatMessage> Messages { get; }

    public ObservableCollection<TaskItem> GeneratedTasks { get; }

    public string MessageText
    {
        get => messageText;
        set => SetProperty(ref messageText, value);
    }

    public new bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetProperty(ref isBusy, value))
            {
                ((Command)SendCommand).ChangeCanExecute();
                ((Command)GenerateTasksCommand).ChangeCanExecute();
            }
        }
    }

    public bool ShowMakeTaskSheet
    {
        get => showMakeTaskSheet;
        set => SetProperty(ref showMakeTaskSheet, value);
    }

    public string MakeTaskTitle
    {
        get => makeTaskTitle;
        set
        {
            if (SetProperty(ref makeTaskTitle, value))
            {
                NotifyPropertyChanged(nameof(CanConfirmMakeTask));
                ((Command)ConfirmMakeTaskCommand).ChangeCanExecute();
            }
        }
    }

    public string MakeTaskOwner
    {
        get => makeTaskOwner;
        set => SetProperty(ref makeTaskOwner, value);
    }

    public DateTime MakeTaskWhen
    {
        get => makeTaskWhen;
        set => SetProperty(ref makeTaskWhen, value);
    }

    public string MakeTaskPriority
    {
        get => makeTaskPriority;
        set => SetProperty(ref makeTaskPriority, value);
    }

    public bool CanConfirmMakeTask => !string.IsNullOrWhiteSpace(MakeTaskTitle);

    public bool IsExtractedExpanded
    {
        get => isExtractedExpanded;
        set
        {
            if (SetProperty(ref isExtractedExpanded, value))
                NotifyPropertyChanged(nameof(ExtractedToggleLabel));
        }
    }

    public string ExtractedToggleLabel => IsExtractedExpanded ? "Hide" : "Show";

    public ICommand SendCommand { get; }

    public ICommand GenerateTasksCommand { get; }

    public ICommand MakeTaskFromBubbleCommand { get; }
    public ICommand DismissMakeTaskCommand { get; }
    public ICommand ConfirmMakeTaskCommand { get; }
    public ICommand ConfirmAllTasksCommand { get; }
    public ICommand CycleMakeTaskPriorityCommand { get; }
    public ICommand ToggleExtractedExpandedCommand { get; }

    public bool HasGeneratedTasks => GeneratedTasks.Count > 0;
    public string ConfirmAllLabel => GeneratedTasks.Count == 1
        ? "Save 1 task to board"
        : $"Save {GeneratedTasks.Count} tasks to board";

    public event EventHandler<TaskItem>? TaskAdded;

    public Task InitializeAsync()
    {
        // Preserve conversation across navigations — only seed the welcome prompt once.
        if (initialized) return Task.CompletedTask;
        initialized = true;

        Messages.Add(new ChatMessage
        {
            Sender = "Assistant",
            Message = "Paste a chat, describe a situation, or just tell me what the family needs to get done — I'll turn it into tasks.",
            Timestamp = DateTime.Now
        });
        return Task.CompletedTask;
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    public void SetMakeTaskOwner(string owner)
    {
        if (string.IsNullOrEmpty(owner)) return;
        MakeTaskOwner = owner;
    }

    public void SetMakeTaskWhen(DateTime when)
    {
        MakeTaskWhen = when;
    }

    private void OpenMakeTaskSheet(ChatMessage? message)
    {
        if (message == null) return;
        MakeTaskTitle = message.Message;
        MakeTaskOwner = message.Sender ?? string.Empty;
        MakeTaskWhen = DateTime.Now.AddHours(2);
        MakeTaskPriority = "Med";
        ShowMakeTaskSheet = true;
    }

    private void CyclePriority()
    {
        MakeTaskPriority = MakeTaskPriority switch
        {
            "Low" => "Med",
            "Med" => "High",
            _ => "Low"
        };
    }

    private async Task ConfirmMakeTaskAsync()
    {
        if (!CanConfirmMakeTask) return;

        var task = new TaskItem
        {
            Title = MakeTaskTitle.Trim(),
            Owner = MakeTaskOwner,
            DueDate = MakeTaskWhen,
            Priority = MakeTaskPriority,
            Points = 15
        };

        ShowMakeTaskSheet = false;

        var createdIds = await aiService.ConfirmDraftTasksAsync(new[] { task });
        if (createdIds.Count == 0)
        {
            await AppDialog.ShowAlertAsync("Save failed", "Could not save the task to the board. Please try again.");
            return;
        }

        GeneratedTasks.Insert(0, task);
        TaskAdded?.Invoke(this, task);
    }

    private async Task SendAsync()
    {
        var prompt = MessageText.Trim();
        if (string.IsNullOrEmpty(prompt)) return;

        Messages.Add(new ChatMessage { Sender = "You", Message = prompt, Timestamp = DateTime.Now, IsUser = true });
        MessageText = string.Empty;

        await GenerateTasksAsync(prompt);
    }

    private async Task GenerateTasksAsync(string? prompt = null)
    {
        if (IsBusy) return;
        IsBusy = true;

        // Build context: use the explicit prompt or fall back to the full conversation text.
        var context = string.IsNullOrWhiteSpace(prompt)
            ? string.Join("\n", Messages.Select(m => $"{m.Sender}: {m.Message}"))
            : prompt;

        try
        {
            List<TaskItem> drafts;
            try
            {
                drafts = (await aiService.GetAssistantDraftTasksAsync(context)).ToList();
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[ChatToTask] auth failure: {ex.Message}");
                Messages.Add(new ChatMessage
                {
                    Sender = "Assistant",
                    Message = "Your session looks expired. Please sign in again and retry.",
                    Timestamp = DateTime.Now
                });
                return;
            }
            catch (Exception ex)
            {
                // Real failure (network / 5xx / parse) — surface it distinctly from
                // a successful-but-empty result so the user knows to retry.
                Debug.WriteLine($"[ChatToTask] GenerateTasksAsync failed: {ex}");
                Messages.Add(new ChatMessage
                {
                    Sender = "Assistant",
                    Message = "Sorry, I couldn't reach the assistant right now. Check your connection and try again.",
                    Timestamp = DateTime.Now
                });
                return;
            }

            var reply = drafts.Count > 0
                ? $"Got it — I found {drafts.Count} task{(drafts.Count == 1 ? "" : "s")} from that:"
                : "I couldn't identify any specific tasks. Try being more specific about who should do what and when.";

            Messages.Add(new ChatMessage { Sender = "Assistant", Message = reply, Timestamp = DateTime.Now });

            GeneratedTasks.Clear();
            foreach (var task in drafts) GeneratedTasks.Add(task);
            NotifyGeneratedTasksChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmAllTasksAsync()
    {
        if (GeneratedTasks.Count == 0) return;

        IsBusy = true;
        var tasks = GeneratedTasks.ToList();
        try
        {
            var createdIds = await aiService.ConfirmDraftTasksAsync(tasks);
            if (createdIds.Count == 0)
            {
                await AppDialog.ShowAlertAsync("Save failed", "Could not save tasks to the board. Please try again.");
                return;
            }

            GeneratedTasks.Clear();
            NotifyGeneratedTasksChanged();
            foreach (var t in tasks) TaskAdded?.Invoke(this, t);

            Messages.Add(new ChatMessage
            {
                Sender = "Assistant",
                Message = $"Added {createdIds.Count} task{(createdIds.Count == 1 ? "" : "s")} to the board.",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NotifyGeneratedTasksChanged()
    {
        NotifyPropertyChanged(nameof(HasGeneratedTasks));
        NotifyPropertyChanged(nameof(ConfirmAllLabel));
        ((Command)ConfirmAllTasksCommand).ChangeCanExecute();
    }
}
