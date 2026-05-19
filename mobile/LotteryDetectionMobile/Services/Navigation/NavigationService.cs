using System.Diagnostics;
using LotteryDetectionMobile.Views.Dashboard;
using LotteryDetectionMobile.Views.Family;
using LotteryDetectionMobile.Views.Forms;
using LotteryDetectionMobile.Views.LotteryCapture;

namespace LotteryDetectionMobile.Services.Navigation;

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
            "task" => NavigateToRootAsync(nameof(MyTasksPage)),
            "mic" => NavigateToRootAsync(nameof(LotteryCapturePage)),
            "you" => NavigateToRootAsync(nameof(GamificationPage)),
            "history" => NavigateToRootAsync(nameof(CalendarPage)),
            "settings" => NavigateToRootAsync(nameof(SettingsPage)),
            _ => Task.CompletedTask
        };
    }

    public async Task NavigateToAITaskAssistantAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(AITaskAssistantPage));
    }

    public async Task NavigateToFamilyBoardAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(FamilyLiveBoardPage));
    }

    public async Task NavigateToChatToTaskAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(ChatToTaskPage));
    }

    public async Task NavigateToCalendarAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(CalendarPage));
    }

    public async Task NavigateToGamificationAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(GamificationPage));
    }

    public async Task NavigateToTaskDetailAsync(string taskId, bool editMode = false)
    {
        if (Shell.Current == null) return;
        var parameters = new Dictionary<string, object> { { "TaskId", taskId } };
        if (editMode) parameters["EditMode"] = true;
        await Shell.Current.GoToAsync(nameof(TaskDetailPage), parameters);
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

    public async Task NavigateToAdminAsync(bool openInvite = false)
    {
        if (Shell.Current == null) return;
        if (openInvite)
        {
            var parameters = new Dictionary<string, object> { { "OpenInvite", true } };
            await Shell.Current.GoToAsync(nameof(AdminRoleManagementPage), parameters);
            return;
        }
        await Shell.Current.GoToAsync(nameof(AdminRoleManagementPage));
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

    public async Task NavigateToMyTasksAsync()
    {
        if (Shell.Current == null) return;
        await Shell.Current.GoToAsync(nameof(MyTasksPage));
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
