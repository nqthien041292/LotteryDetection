using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.LotteryHistory;

public partial class LotteryHistoryPage : ContentPage
{
    private LotteryHistoryViewModel? vm;

    public LotteryHistoryPage()
    {
        InitializeComponent();

        var service = MauiProgram.Services?.GetService<ILotteryHistoryService>();
        if (service != null)
        {
            vm = new LotteryHistoryViewModel(service, NavigationService.Default);
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
