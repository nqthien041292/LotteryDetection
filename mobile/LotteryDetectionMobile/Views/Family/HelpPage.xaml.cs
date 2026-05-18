using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Family;

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
