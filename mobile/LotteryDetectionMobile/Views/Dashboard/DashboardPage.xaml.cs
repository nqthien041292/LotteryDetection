using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = new DashboardViewModel();
    }
}
