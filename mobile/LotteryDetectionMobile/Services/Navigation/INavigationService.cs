namespace LotteryDetectionMobile.Services.Navigation;

public interface INavigationService
{
    Task NavigateToDashboardAsync();
    Task NavigateToRootTabAsync(string? tabKey);
    Task NavigateToAITaskAssistantAsync();
    Task NavigateToFamilyBoardAsync();
    Task NavigateToChatToTaskAsync();
    Task NavigateToCalendarAsync();
    Task NavigateToGamificationAsync();
    Task NavigateToTaskDetailAsync(string taskId, bool editMode = false);
    Task NavigateToNotificationsAsync();
    Task NavigateToSettingsAsync();
    Task NavigateToHelpAsync();
    Task NavigateToAdminAsync(bool openInvite = false);
    Task NavigateToLoginWithSocialAsync();
    Task NavigateToLotteryCaptureAsync();
    Task NavigateToLotteryResultsAsync();
    Task NavigateToLotteryHistoryAsync();
    Task NavigateToMyTasksAsync();
    Task NavigateBackAsync();
}
