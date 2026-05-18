using FluentAssertions;

namespace LotteryDetectionMobile.Tests.ViewModel;

/// <summary>
///     Tests for the cold-start splash routing decision
///     (mirrors SplashViewModel.ResolveDestinationAsync).
/// </summary>
public class SplashRoutingLogicTests
{
    private static Task<bool> Restore(bool result) => Task.FromResult(result);

    [Fact]
    public async Task OnboardingNotCompleted_RoutesToOnboarding()
    {
        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: false,
            isSignedIn: false,
            tryRestoreSession: () => Restore(false));

        dest.Should().Be(StartupDestination.Onboarding);
    }

    [Fact]
    public async Task OnboardedAndSignedIn_RoutesToDashboard_WithoutRestoring()
    {
        var restoreCalled = false;

        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: true,
            isSignedIn: true,
            tryRestoreSession: () =>
            {
                restoreCalled = true;
                return Restore(false);
            });

        dest.Should().Be(StartupDestination.Dashboard);
        restoreCalled.Should().BeFalse("a live session must not trigger a redundant restore");
    }

    [Fact]
    public async Task OnboardedNotSignedIn_RestoreSucceeds_RoutesToDashboard()
    {
        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: true,
            isSignedIn: false,
            tryRestoreSession: () => Restore(true));

        dest.Should().Be(StartupDestination.Dashboard);
    }

    [Fact]
    public async Task OnboardedNotSignedIn_RestoreFails_RoutesToLogin()
    {
        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: true,
            isSignedIn: false,
            tryRestoreSession: () => Restore(false));

        dest.Should().Be(StartupDestination.Login);
    }

    [Fact]
    public async Task RestoreExceedsTimeout_RoutesToLogin_WithoutWaitingForRestore()
    {
        var start = DateTime.UtcNow;

        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: true,
            isSignedIn: false,
            tryRestoreSession: async () =>
            {
                await Task.Delay(5000);
                return true;
            },
            restoreTimeoutMs: 200);

        var elapsed = DateTime.UtcNow - start;

        dest.Should().Be(StartupDestination.Login);
        elapsed.TotalMilliseconds.Should().BeLessThan(2000,
            "the timeout must short-circuit instead of waiting for the hung restore");
    }

    [Fact]
    public async Task RestoreThrows_RoutesToLogin()
    {
        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: true,
            isSignedIn: false,
            tryRestoreSession: () => throw new InvalidOperationException("network down"));

        dest.Should().Be(StartupDestination.Login);
    }

    [Fact]
    public async Task RestoreThrowsAsync_RoutesToLogin()
    {
        var dest = await SplashRoutingLogic.ResolveDestinationAsync(
            onboardingCompleted: true,
            isSignedIn: false,
            tryRestoreSession: async () =>
            {
                await Task.Yield();
                throw new Exception("token endpoint error");
            });

        dest.Should().Be(StartupDestination.Login);
    }
}
