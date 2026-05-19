using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.Family;

public partial class GamificationPage : ContentPage
{
    public GamificationPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "You";

        var gamifService = MauiProgram.Services?.GetService<IGamificationService>();
        var rewardService = MauiProgram.Services?.GetService<IRewardService>();
        var authService = MauiProgram.Services?.GetService<IAuthService>();
        var memberCache = MauiProgram.Services?.GetService<IFamilyMemberCache>();
        if (gamifService != null && rewardService != null)
            BindingContext = new GamificationViewModel(NavigationService.Default, gamifService, rewardService, authService, memberCache);
    }

    private GamificationViewModel? ViewModel => BindingContext as GamificationViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BottomBar.SelectedTab = "You";
        if (ViewModel != null) await ViewModel.InitializeAsync();
    }

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