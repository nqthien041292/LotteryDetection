using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = new DashboardViewModel();
    }
}
