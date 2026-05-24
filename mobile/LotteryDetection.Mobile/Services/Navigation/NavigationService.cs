using System.Diagnostics;
using LotteryDetection.Mobile.Views.Dashboard;
using LotteryDetection.Mobile.Views.Family;
using LotteryDetection.Mobile.Views.Forms;
using LotteryDetection.Mobile.Views.LotteryCapture;
using LotteryDetection.Mobile.Views.LotteryHistory;
using LotteryDetection.Mobile.Views.LotteryResults;

namespace LotteryDetection.Mobile.Services.Navigation;

public class NavigationService : INavigationService
{
    public static INavigationService Default { get; } = new NavigationService();

    public async Task NavigateToDashboardAsync()
    {
        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(NavigateToDashboardAsync);
            return;
        }
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
    }

    public Task NavigateToRootTabAsync(string? tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey))
            return Task.CompletedTask;

        return tabKey.ToLowerInvariant() switch
        {
            "home" => NavigateToDashboardAsync(),
            "mic" => NavigateToRootAsync(nameof(LotteryCapturePage)),
            "settings" => NavigateToRootAsync(nameof(SettingsPage)),
            _ => Task.CompletedTask
        };
    }

    public async Task NavigateToNotificationsAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(NotificationsPage));
    }

    public async Task NavigateToSettingsAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    public async Task NavigateToHelpAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(HelpPage));
    }

    public async Task NavigateToLoginWithSocialAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    public async Task NavigateToLotteryCaptureAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(LotteryCapturePage));
    }

    public async Task NavigateToLotteryResultsAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(LotteryResultsPage));
    }

    public async Task NavigateToLotteryHistoryAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(LotteryHistoryPage));
    }

    public async Task NavigateToLotteryLiveResultsAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync("LotteryLiveResultsPage");
    }

    private async Task NavigateToRootAsync(string route)
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}/{route}");
    }

    public async Task NavigateBackAsync()
    {
        if (Shell.Current == null) return;

        async Task DoNavigateBack()
        {
            // PopAsync directly manipulates the navigation stack and reliably returns
            // to the page that opened the current child route.
            try
            {
                await Shell.Current.Navigation.PopAsync();
            }
            catch
            {
                try
                {
                    await Shell.Current.GoToAsync("..");
                }
                catch
                {
                    await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
                }
            }
        }

        try
        {
            if (!MainThread.IsMainThread)
                await MainThread.InvokeOnMainThreadAsync(DoNavigateBack);
            else
                await DoNavigateBack();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NavigationService] NavigateBackAsync failed: {ex.Message}");
        }
    }
}
