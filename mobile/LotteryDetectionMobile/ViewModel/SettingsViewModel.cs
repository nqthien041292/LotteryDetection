using System.Collections.ObjectModel;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using System.Collections.Generic;

namespace LotteryDetectionMobile.ViewModel;

public class SettingsViewModel : TabNavigationViewModel
{
    private const string DisplayNameOverrideKey = "user_display_name_override";

    private bool dataShare;
    private bool darkMode;
    private bool notifMorning = true;
    private bool notifReminders = true;
    private bool notifDigest;
    private string persona = "warm";
    private bool voiceWake = true;
    private bool voiceCorrect = true;

    private string userName = string.Empty;
    private string userEmail = string.Empty;
    private string planName = "Family Plan";
    private string planRenewal = string.Empty;
    private bool hasLoaded;
    private bool isLoading;

    private readonly IAuthService? authService;
    private readonly IFamilyService? familyService;
    private readonly IProfileService? profileService;
    private bool _loadingPrefs;

    public SettingsViewModel()
        : this(NavigationService.Default, null, null, null)
    {
    }

    public SettingsViewModel(
        INavigationService navigationService,
        IAuthService? authService,
        IFamilyService? familyService,
        IProfileService? profileService = null)
        : base(navigationService)
    {
        this.authService = authService;
        this.familyService = familyService;
        this.profileService = profileService;

        TogglePushCommand = new Command(() => NotifMorning = !NotifMorning);
        ToggleCalendarSyncCommand = new Command(() => NotifReminders = !NotifReminders);
        ToggleVoiceHintsCommand = new Command(() => VoiceWake = !VoiceWake);
        SetPersonaCommand = new Command<string>(SetPersona);
        EditUserNameCommand = new Command(async () => await EditUserNameAsync());

        Members = new ObservableCollection<SettingsMember>();
    }

    public ObservableCollection<SettingsMember> Members { get; }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public string UserName
    {
        get => userName;
        set => SetProperty(ref userName, value);
    }

    public string UserEmail
    {
        get => userEmail;
        set => SetProperty(ref userEmail, value);
    }

    public string PlanName
    {
        get => planName;
        set => SetProperty(ref planName, value);
    }

    public string PlanRenewal
    {
        get => planRenewal;
        set => SetProperty(ref planRenewal, value);
    }

    public string Persona
    {
        get => persona;
        set
        {
            if (SetProperty(ref persona, value))
            {
                NotifyPropertyChanged(nameof(IsPersonaWarm));
                NotifyPropertyChanged(nameof(IsPersonaDirect));
                NotifyPropertyChanged(nameof(IsPersonaPlayful));
            }
        }
    }

    public bool IsPersonaWarm => persona == "warm";
    public bool IsPersonaDirect => persona == "direct";
    public bool IsPersonaPlayful => persona == "playful";

    public bool NotifMorning
    {
        get => notifMorning;
        set { if (SetProperty(ref notifMorning, value) && !_loadingPrefs) PersistPreferences(); }
    }

    public bool NotifReminders
    {
        get => notifReminders;
        set { if (SetProperty(ref notifReminders, value) && !_loadingPrefs) PersistPreferences(); }
    }

    public bool NotifDigest
    {
        get => notifDigest;
        set { if (SetProperty(ref notifDigest, value) && !_loadingPrefs) PersistPreferences(); }
    }

    public bool VoiceWake
    {
        get => voiceWake;
        set { if (SetProperty(ref voiceWake, value) && !_loadingPrefs) PersistPreferences(); }
    }

    public bool VoiceCorrect
    {
        get => voiceCorrect;
        set { if (SetProperty(ref voiceCorrect, value) && !_loadingPrefs) PersistPreferences(); }
    }

    public bool DarkMode
    {
        get => darkMode;
        set
        {
            if (SetProperty(ref darkMode, value))
            {
                if (Application.Current != null)
                    Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
                Preferences.Set("app_dark_mode", value);
            }
        }
    }

    public bool DataShare
    {
        get => dataShare;
        set => SetProperty(ref dataShare, value);
    }

    // Legacy aliases — keep so older XAML pages bound to these still compile.
    public bool PushEnabled
    {
        get => NotifMorning;
        set => NotifMorning = value;
    }

    public bool CalendarSyncEnabled
    {
        get => NotifReminders;
        set => NotifReminders = value;
    }

    public bool VoiceHintsEnabled
    {
        get => VoiceWake;
        set => VoiceWake = value;
    }

    public ICommand TogglePushCommand { get; }
    public ICommand ToggleCalendarSyncCommand { get; }
    public ICommand ToggleVoiceHintsCommand { get; }
    public ICommand SetPersonaCommand { get; }
    public ICommand EditUserNameCommand { get; }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    public async Task InitializeAsync()
    {
        IsLoading = !hasLoaded;
        // Sync dark mode toggle from saved preference (use backing field to avoid side effects)
        try
        {
            darkMode = Preferences.Get("app_dark_mode", Application.Current?.UserAppTheme == AppTheme.Dark);
            NotifyPropertyChanged(nameof(DarkMode));

            // 1. User identity from auth token (server is source of truth — keep cross-device renames in sync)
            var email = authService?.UserEmail;
            UserEmail = string.IsNullOrWhiteSpace(email) ? string.Empty : email;

        // Show cached name immediately so the screen doesn't flash "User" while the network call runs.
        var cachedName = Preferences.Get(DisplayNameOverrideKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(cachedName))
        {
            UserName = cachedName;
        }
        else
        {
            var displayName = authService?.UserDisplayName;
            UserName = string.IsNullOrWhiteSpace(displayName) ? "User" : FormatDisplayName(displayName);
        }

        // Then reconcile with the server: if a different device renamed the user, pick that up.
        if (profileService != null)
        {
            try
            {
                var serverName = await profileService.GetDisplayNameAsync();
                if (!string.IsNullOrWhiteSpace(serverName) && serverName != UserName)
                {
                    UserName = serverName;
                    Preferences.Set(DisplayNameOverrideKey, serverName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Failed to fetch server display name: {ex.Message}");
            }

            try
            {
                var prefs = await profileService.GetPreferencesAsync();
                if (prefs != null)
                {
                    _loadingPrefs = true;
                    try
                    {
                        notifMorning = prefs.NotifMorning;
                        notifReminders = prefs.NotifReminders;
                        notifDigest = prefs.NotifDigest;
                        voiceWake = prefs.VoiceWake;
                        voiceCorrect = prefs.VoiceCorrect;
                        persona = prefs.Persona;

                        NotifyPropertyChanged(nameof(NotifMorning));
                        NotifyPropertyChanged(nameof(NotifReminders));
                        NotifyPropertyChanged(nameof(NotifDigest));
                        NotifyPropertyChanged(nameof(VoiceWake));
                        NotifyPropertyChanged(nameof(VoiceCorrect));
                        NotifyPropertyChanged(nameof(Persona));
                        NotifyPropertyChanged(nameof(IsPersonaWarm));
                        NotifyPropertyChanged(nameof(IsPersonaDirect));
                        NotifyPropertyChanged(nameof(IsPersonaPlayful));
                    }
                    finally
                    {
                        _loadingPrefs = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Failed to load preferences: {ex.Message}");
            }
        }

            // 2. Family roster from API
            if (familyService == null) return;
            try
            {
                var members = await familyService.GetMembersAsync();
                var currentMember = members.FirstOrDefault(m =>
                    string.Equals(m.Email, UserEmail, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(currentMember?.Name) && currentMember.Name != UserName)
                {
                    UserName = currentMember.Name;
                    Preferences.Set(DisplayNameOverrideKey, currentMember.Name);
                }

                Members.Clear();
                foreach (var m in members)
                {
                    Members.Add(new SettingsMember
                    {
                        Id = m.Id ?? string.Empty,
                        Name = m.Name ?? string.Empty,
                        Role = m.Role ?? "Member",
                        Level = 0,
                        Badge = string.Equals(m.Role, "Owner", StringComparison.OrdinalIgnoreCase) ? "ADMIN" : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Failed to load members: {ex.Message}");
            }
        }
        finally
        {
            hasLoaded = true;
            IsLoading = false;
        }
    }

    private void PersistPreferences()
    {
        if (profileService == null) return;
        _ = profileService.SavePreferencesAsync(new UserPreferencesData
        {
            NotifMorning = notifMorning,
            NotifReminders = notifReminders,
            NotifDigest = notifDigest,
            VoiceWake = voiceWake,
            VoiceCorrect = voiceCorrect,
            Persona = persona
        });
    }

    private static string FormatDisplayName(string raw)
    {
        // Email-localpart "alex.chen" -> "Alex Chen"
        var parts = raw.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p =>
            string.IsNullOrEmpty(p) ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private void SetPersona(string? key)
    {
        if (string.IsNullOrEmpty(key)) return;
        Persona = key.ToLowerInvariant();
        if (!_loadingPrefs) PersistPreferences();
    }

    private async Task EditUserNameAsync()
    {
        var entered = await AppDialog.ShowPromptAsync(
            title: "Edit name",
            message: "Enter your display name",
            accept: "Save",
            cancel: "Cancel",
            initialValue: UserName,
            placeholder: "Your name",
            maxLength: 64);

        if (entered == null) return;
        var trimmed = entered.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == UserName) return;

        var previousName = UserName;
        UserName = trimmed;
        Preferences.Set(DisplayNameOverrideKey, trimmed);

        if (profileService == null) return;
        try
        {
            await profileService.UpdateDisplayNameAsync(trimmed);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Profile update failed: {ex.Message}");
            UserName = previousName;
            Preferences.Set(DisplayNameOverrideKey, previousName);
            await AppDialog.ShowAlertAsync("Save failed", "Could not save your display name. Please try again.");
        }
    }
}

public class SettingsMember
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string Subtitle => $"{Role} · Lvl {Level}";
    public bool HasBadge => !string.IsNullOrEmpty(Badge);
}
