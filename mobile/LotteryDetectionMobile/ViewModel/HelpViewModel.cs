using System.Collections.ObjectModel;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Models.Help;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public class HelpViewModel : TabNavigationViewModel
{
    public static readonly string[] TopicOptions =
    {
        "General question",
        "Bug or crash",
        "Account & billing",
        "Privacy",
        "Feedback"
    };

    private const int ToastDurationMs = 2200;

    public ObservableCollection<TopicChip> TopicChips { get; }

    private readonly IHelpTicketService ticketService;
    private string activeTab = "faq";
    private string contactMessage = string.Empty;
    private string contactTopic = "General question";
    private bool isSending;
    private bool showToast;
    private int toastVersion;

    public HelpViewModel()
        : this(NavigationService.Default, MockHelpTicketService.Instance)
    {
    }

    public HelpViewModel(INavigationService navigationService, IHelpTicketService ticketService)
        : base(navigationService)
    {
        this.ticketService = ticketService;

        TopicChips = new ObservableCollection<TopicChip>();
        foreach (var t in TopicOptions)
            TopicChips.Add(new TopicChip { Label = t, IsSelected = t == contactTopic });

        Faq = new ObservableCollection<FaqItem>
        {
            new()
            {
                Question = "How do I add a family member?",
                Answer = "Open Settings → Family & roles → Invite. They'll get a link to sign in with their Microsoft account, and you choose their role (Parent, Co-parent, Teen, or Kid).",
                IsOpen = true
            },
            new()
            {
                Question = "What does the AI hear?",
                Answer = "Voice capture is fully on-device. Audio never leaves your phone. The AI only sees the transcribed text once you tap Save."
            },
            new()
            {
                Question = "Can I share a Google Calendar?",
                Answer = "Yes — Settings → Connected accounts → Google. Tasks tagged with a date appear on both sides; edits sync within a minute."
            },
            new()
            {
                Question = "How do streaks work?",
                Answer = "A streak grows when at least one family member completes any task each day. Miss a day and the streak resets — but the household keeps its level and badges."
            },
            new()
            {
                Question = "Are kids' tasks private?",
                Answer = "A teen's tasks are visible to parents and co-parents by default. Kids only see tasks assigned to them or marked Shared. Adjust under Permissions & sharing."
            },
            new()
            {
                Question = "How do I cancel?",
                Answer = "Settings → Plan → Manage. Cancellations apply at the end of your current billing cycle, and your data stays for 90 days in case you come back."
            }
        };

        Tutorials = new ObservableCollection<Tutorial>
        {
            new()
            {
                Title = "Capture your first voice task",
                Duration = "0:42",
                Url = "https://familyai.app/tutorials/voice",
                TintColor = "#E0EAFF",
                ForegroundColor = "#1E5BFF",
                IconGlyph = "🎙"
            },
            new()
            {
                Title = "Set up the family board",
                Duration = "1:18",
                Url = "https://familyai.app/tutorials/board",
                Accent = "primary",
                TintColor = "#D1F0EC",
                ForegroundColor = "#0F766E",
                IconGlyph = "📋"
            },
            new()
            {
                Title = "Convert chat messages into tasks",
                Duration = "0:55",
                Url = "https://familyai.app/tutorials/chat",
                TintColor = "#FFE4DD",
                ForegroundColor = "#B23A1A",
                IconGlyph = "✨"
            },
            new()
            {
                Title = "Sync with Google Calendar",
                Duration = "1:04",
                Url = "https://familyai.app/tutorials/calendar",
                TintColor = "#D6EEFB",
                ForegroundColor = "#0E5A8A",
                IconGlyph = "📅"
            },
            new()
            {
                Title = "Earn streaks & badges as a family",
                Duration = "1:33",
                Url = "https://familyai.app/tutorials/streaks",
                TintColor = "#E1DDFE",
                ForegroundColor = "#4C1D95",
                IconGlyph = "🏁"
            }
        };

        SetTabCommand = new Command<string>(tab =>
        {
            if (string.IsNullOrEmpty(tab)) return;
            ActiveTab = tab;
        });
        ToggleFaqCommand = new Command<FaqItem>(ToggleFaq);
        OpenTutorialCommand = new Command<Tutorial>(OpenTutorial);
        SetTopicCommand = new Command<object>(arg =>
        {
            var topic = arg switch
            {
                TopicChip chip => chip.Label,
                string s => s,
                _ => null
            };
            if (string.IsNullOrEmpty(topic)) return;
            ContactTopic = topic;
        });
        SendContactCommand = new Command(async () => await SendContactAsync(), () => CanSend);
    }

    public ObservableCollection<FaqItem> Faq { get; }
    public ObservableCollection<Tutorial> Tutorials { get; }

    public ICommand SetTabCommand { get; }
    public ICommand ToggleFaqCommand { get; }
    public ICommand OpenTutorialCommand { get; }
    public ICommand SetTopicCommand { get; }
    public ICommand SendContactCommand { get; }

    public string ActiveTab
    {
        get => activeTab;
        set
        {
            if (SetProperty(ref activeTab, value))
                NotifyTabStates();
        }
    }

    public bool IsFaqTab => ActiveTab == "faq";
    public bool IsTutorialsTab => ActiveTab == "tutorials";
    public bool IsContactTab => ActiveTab == "contact";

    public string ContactTopic
    {
        get => contactTopic;
        set
        {
            if (SetProperty(ref contactTopic, value))
            {
                foreach (var chip in TopicChips)
                    chip.IsSelected = chip.Label == value;
            }
        }
    }

    public string ContactMessage
    {
        get => contactMessage;
        set
        {
            if (SetProperty(ref contactMessage, value))
            {
                NotifyPropertyChanged(nameof(CanSend));
                NotifyPropertyChanged(nameof(MessageCounter));
                ((Command)SendContactCommand).ChangeCanExecute();
            }
        }
    }

    public bool IsSending
    {
        get => isSending;
        set
        {
            if (SetProperty(ref isSending, value))
            {
                NotifyPropertyChanged(nameof(CanSend));
                ((Command)SendContactCommand).ChangeCanExecute();
            }
        }
    }

    public bool CanSend => !IsSending && !string.IsNullOrWhiteSpace(ContactMessage);

    public string MessageCounter => $"{(ContactMessage?.Length ?? 0)}/2000";

    public bool ShowToast
    {
        get => showToast;
        set => SetProperty(ref showToast, value);
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    public void SetInitialTab(string? tab)
    {
        if (string.IsNullOrEmpty(tab)) return;
        ActiveTab = tab;
    }

    private void ToggleFaq(FaqItem? target)
    {
        if (target == null) return;
        var shouldOpen = !target.IsOpen;
        foreach (var item in Faq) item.IsOpen = false;
        if (shouldOpen) target.IsOpen = true;
    }

    private async void OpenTutorial(Tutorial? tutorial)
    {
        if (tutorial == null || string.IsNullOrEmpty(tutorial.Url)) return;
        try
        {
            await Browser.OpenAsync(tutorial.Url, BrowserLaunchMode.External);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpViewModel] OpenTutorial failed: {ex.Message}");
        }
    }

    private async Task SendContactAsync()
    {
        if (!CanSend) return;
        IsSending = true;
        try
        {
            var ticket = new HelpTicket
            {
                Topic = ContactTopic,
                Message = ContactMessage.Trim()
            };
            var ok = await ticketService.SubmitAsync(ticket);
            if (ok)
            {
                ContactMessage = string.Empty;
                _ = FlashToastAsync();
            }
        }
        finally
        {
            IsSending = false;
        }
    }

    private async Task FlashToastAsync()
    {
        var ticket = ++toastVersion;
        ShowToast = true;
        await Task.Delay(ToastDurationMs);
        if (ticket == toastVersion) ShowToast = false;
    }

    private void NotifyTabStates()
    {
        NotifyPropertyChanged(nameof(IsFaqTab));
        NotifyPropertyChanged(nameof(IsTutorialsTab));
        NotifyPropertyChanged(nameof(IsContactTab));
    }
}
