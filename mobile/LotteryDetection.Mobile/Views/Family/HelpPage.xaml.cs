using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.Family;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Settings";

        var ticketService = MauiProgram.Services?.GetService<IHelpTicketService>();
        if (ticketService != null)
            BindingContext = new HelpViewModel(NavigationService.Default, ticketService);
    }

    private HelpViewModel? ViewModel => BindingContext as HelpViewModel;

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}
