using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.LotteryResults;

public partial class LotteryLiveResultsPage : ContentPage
{
    private LotteryLiveResultsViewModel? vm;
    private CancellationTokenSource? animationCts;

    public LotteryLiveResultsPage()
    {
        InitializeComponent();

        var service = MauiProgram.Services?.GetService<ILotteryResultsService>();
        if (service != null)
        {
            vm = new LotteryLiveResultsViewModel(service, NavigationService.Default);
            BindingContext = vm;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (vm != null)
        {
            await vm.InitializeAsync();
            vm.StartAutoRefresh();
            StartLiveBadgeAnimation();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (vm != null)
        {
            vm.StopAutoRefresh();
        }
        StopLiveBadgeAnimation();
    }

    private void StartLiveBadgeAnimation()
    {
        StopLiveBadgeAnimation();

        animationCts = new CancellationTokenSource();
        var token = animationCts.Token;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await LiveBadgeBorder.FadeTo(0.4, 600);
                    await LiveBadgeBorder.FadeTo(1.0, 600);
                }
                catch
                {
                    break;
                }
            }
        });
    }

    private void StopLiveBadgeAnimation()
    {
        animationCts?.Cancel();
        animationCts = null;
        LiveBadgeBorder.Opacity = 1.0;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
