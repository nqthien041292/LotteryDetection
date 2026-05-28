using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Views.Settings;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel vm;

    public SettingsPage()
    {
        InitializeComponent();
        vm = new SettingsViewModel();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        vm.RefreshAvatar();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
