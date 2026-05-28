namespace LotteryDetection.Mobile.Services.Navigation;

public interface INavigationService
{
    Task NavigateToLoginWithSocialAsync();
    Task NavigateToDashboardAsync();
    Task NavigateToLotteryCaptureAsync();
    Task NavigateToLotteryResultsAsync();
    Task NavigateToLotteryHistoryAsync();
    Task NavigateToSettingsAsync();
    Task NavigateBackAsync();
}
