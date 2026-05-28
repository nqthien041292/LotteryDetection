using LotteryDetection.Mobile.Services.Auth;

namespace LotteryDetection.Mobile.Services.Mock;

public sealed class MockAuthService : IAuthService
{
    public static IAuthService Instance { get; } = new MockAuthService();

    public bool IsSignedIn => true;
    public string? UserDisplayName => "Lottery Demo";
    public string? UserEmail => "demo@lotterydetection.local";

    public Task<string> SignInAsync() => Task.FromResult("mock-access-token");

    public Task<string> SignInExternalAsync(string provider, string providerKey, string providerAccessCode, string? displayName = null) =>
        Task.FromResult("mock-access-token");

    public Task SetDisplayNameAsync(string displayName) => Task.CompletedTask;

    public Task<string> GetAccessTokenAsync() => Task.FromResult("mock-access-token");

    public Task SignOutAsync() => Task.CompletedTask;

    public Task<bool> CanAcquireTokenSilentlyAsync() => Task.FromResult(true);

    public Task<bool> TryRestoreSessionAsync() => Task.FromResult(true);
}
