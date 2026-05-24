namespace LotteryDetection.Mobile.Services.Navigation;

public interface INavigationService
{
    Task NavigateToDashboardAsync();
    Task NavigateToRootTabAsync(string? tabKey);
    Task NavigateToNotificationsAsync();
    Task NavigateToSettingsAsync();
    Task NavigateToHelpAsync();
    Task NavigateToLoginWithSocialAsync();
    Task NavigateToLotteryCaptureAsync();
    Task NavigateToLotteryResultsAsync();
    Task NavigateToLotteryHistoryAsync();
    Task NavigateToLotteryLiveResultsAsync();
    Task NavigateBackAsync();
}
