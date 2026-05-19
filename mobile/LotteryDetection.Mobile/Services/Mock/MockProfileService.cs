using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public sealed class MockProfileService : IProfileService
{
    private string displayName = "Lottery Demo";
    private UserPreferencesData preferences = new();

    public static IProfileService Instance { get; } = new MockProfileService();

    public Task UpdateDisplayNameAsync(string displayName)
    {
        this.displayName = displayName;
        return Task.CompletedTask;
    }

    public Task<string?> GetDisplayNameAsync() => Task.FromResult<string?>(displayName);

    public Task<UserPreferencesData?> GetPreferencesAsync() => Task.FromResult<UserPreferencesData?>(preferences);

    public Task SavePreferencesAsync(UserPreferencesData prefs)
    {
        preferences = prefs;
        return Task.CompletedTask;
    }
}
