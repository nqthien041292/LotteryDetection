using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.LotteryResults;

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
