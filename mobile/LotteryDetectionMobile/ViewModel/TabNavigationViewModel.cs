using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

public abstract class TabNavigationViewModel : BaseViewModel
{
    protected readonly INavigationService navigationService;

    protected TabNavigationViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
    }

    protected Task HandleTabSelectionAsync(string? tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey))
            return Task.CompletedTask;

        return navigationService.NavigateToRootTabAsync(tabKey);
    }
}
