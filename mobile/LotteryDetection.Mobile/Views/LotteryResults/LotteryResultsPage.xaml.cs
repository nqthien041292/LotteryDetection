using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.LotteryResults;

public partial class LotteryResultsPage : ContentPage
{
    private LotteryResultsViewModel? vm;

    public LotteryResultsPage()
    {
        InitializeComponent();

        var service = MauiProgram.Services?.GetService<ILotteryResultsService>();
        if (service != null)
        {
            vm = new LotteryResultsViewModel(service, NavigationService.Default);
            BindingContext = vm;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (vm != null)
            await vm.InitializeAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
