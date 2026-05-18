namespace LotteryDetectionMobile.Services.Interfaces;

public interface IProfileService
{
    Task UpdateDisplayNameAsync(string displayName);

    /// <summary>Returns null when no server record; caller falls back to local claims.</summary>
    Task<string?> GetDisplayNameAsync();

    Task<UserPreferencesData?> GetPreferencesAsync();
    Task SavePreferencesAsync(UserPreferencesData prefs);
}

public class UserPreferencesData
{
    public bool NotifMorning { get; set; } = true;
    public bool NotifReminders { get; set; } = true;
    public bool NotifDigest { get; set; }
    public bool VoiceWake { get; set; } = true;
    public bool VoiceCorrect { get; set; } = true;
    public string Persona { get; set; } = "warm";
}
