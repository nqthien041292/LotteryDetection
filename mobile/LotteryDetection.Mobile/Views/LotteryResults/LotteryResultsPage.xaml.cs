using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.LotteryResults;

public partial class LotteryResultsPage : ContentPage
{
    private LotteryResultsViewModel? vm;

    private CancellationTokenSource? animationCts;

    public LotteryResultsPage()
    {
        InitializeComponent();

        var service = MauiProgram.Services?.GetService<ILotteryResultsService>();
        if (service != null)
        {
            vm = new LotteryResultsViewModel(service, NavigationService.Default);
            BindingContext = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LotteryResultsViewModel.IsAnyActiveRegionLive))
        {
            if (vm != null && vm.IsAnyActiveRegionLive)
            {
                StartLiveAnimation();
            }
            else
            {
                StopLiveAnimation();
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (vm != null)
        {
            await vm.InitializeAsync();
            if (vm.IsAnyActiveRegionLive)
            {
                StartLiveAnimation();
            }
            else
            {
                StopLiveAnimation();
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLiveAnimation();
    }

    private void StartLiveAnimation()
    {
        StopLiveAnimation();

        if (vm == null || !vm.IsAnyActiveRegionLive)
        {
            LiveButtonBorder.Opacity = 1.0;
            return;
        }

        animationCts = new CancellationTokenSource();
        var token = animationCts.Token;

        // Run animation on main thread but handle loops nicely
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Check if still live active, if not reset and exit
                    if (vm == null || !vm.IsAnyActiveRegionLive)
                    {
                        LiveButtonBorder.Opacity = 1.0;
                        break;
                    }

                    await LiveButtonBorder.FadeTo(0.3, 500);
                    await LiveButtonBorder.FadeTo(1.0, 500);
                }
                catch
                {
                    break;
                }
            }
        });
    }

    private void StopLiveAnimation()
    {
        animationCts?.Cancel();
        animationCts = null;
        LiveButtonBorder.Opacity = 1.0;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
