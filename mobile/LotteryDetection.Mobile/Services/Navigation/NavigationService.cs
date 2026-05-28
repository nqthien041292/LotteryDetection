using System.Diagnostics;
using LotteryDetection.Mobile.Views.Dashboard;
using LotteryDetection.Mobile.Views.Forms;
using LotteryDetection.Mobile.Views.LotteryCapture;
using LotteryDetection.Mobile.Views.LotteryHistory;
using LotteryDetection.Mobile.Views.LotteryResults;
using LotteryDetection.Mobile.Views.Settings;
// LotteryLiveResultsPage was removed — live drawing UI now lives inline on LotteryResultsPage.

namespace LotteryDetection.Mobile.Services.Navigation;

public class NavigationService : INavigationService
{
    public static INavigationService Default { get; } = new NavigationService();

    public async Task NavigateToLoginWithSocialAsync()
    {
        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(NavigateToLoginWithSocialAsync);
            return;
        }
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

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

    public async Task NavigateToLotteryCaptureAsync()
    {
        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(NavigateToLotteryCaptureAsync);
            return;
        }
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(LotteryCapturePage));
    }

    public async Task NavigateToLotteryResultsAsync()
    {
        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(NavigateToLotteryResultsAsync);
            return;
        }
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(LotteryResultsPage));
    }

    public async Task NavigateToLotteryHistoryAsync()
    {
        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(NavigateToLotteryHistoryAsync);
            return;
        }
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(LotteryHistoryPage));
    }

    public async Task NavigateToSettingsAsync()
    {
        if (!MainThread.IsMainThread)
        {
            await MainThread.InvokeOnMainThreadAsync(NavigateToSettingsAsync);
            return;
        }
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

public async Task NavigateBackAsync()
    {
        if (Shell.Current == null) return;

        async Task DoNavigateBack()
        {
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
