using System.Windows.Input;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class DashboardViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    public DashboardViewModel()
        : this(NavigationService.Default)
    {
    }

    public DashboardViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;

        OpenLotteryCaptureCommand = new Command(async () => await navigationService.NavigateToLotteryCaptureAsync());
        OpenLotteryHistoryCommand = new Command(async () => await navigationService.NavigateToLotteryHistoryAsync());
        OpenLotteryResultsCommand = new Command(async () => await navigationService.NavigateToLotteryResultsAsync());
    }

    public ICommand OpenLotteryCaptureCommand { get; }
    public ICommand OpenLotteryHistoryCommand { get; }
    public ICommand OpenLotteryResultsCommand { get; }
}
