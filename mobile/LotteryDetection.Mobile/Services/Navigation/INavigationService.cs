namespace LotteryDetection.Mobile.Services.Navigation;

public interface INavigationService
{
    Task NavigateToLoginWithSocialAsync();
    Task NavigateToLotteryCaptureAsync();
    Task NavigateToLotteryResultsAsync();
    Task NavigateToLotteryHistoryAsync();
    Task NavigateToLotteryLiveResultsAsync();
    Task NavigateBackAsync();
}
