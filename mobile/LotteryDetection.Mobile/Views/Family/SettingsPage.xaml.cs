using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.Family;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Settings";

        var auth = MauiProgram.Services?.GetService<IAuthService>();
        var family = MauiProgram.Services?.GetService<IFamilyService>();
        var profile = MauiProgram.Services?.GetService<IProfileService>();
        BindingContext = new SettingsViewModel(NavigationService.Default, auth, family, profile);
    }

    private SettingsViewModel? ViewModel => BindingContext as SettingsViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BottomBar.SelectedTab = "Settings";
        if (ViewModel != null) await ViewModel.InitializeAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnAdminClicked(object sender, TappedEventArgs e)
    {
        await NavigationService.Default.NavigateToAdminAsync();
    }

    private async void OnInviteClicked(object sender, TappedEventArgs e)
    {
        await NavigationService.Default.NavigateToAdminAsync(openInvite: true);
    }

    private async void OnHelpClicked(object sender, TappedEventArgs e)
    {
        await NavigationService.Default.NavigateToHelpAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}