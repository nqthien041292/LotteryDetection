using System.ComponentModel;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Views.LotteryCapture;

public partial class LotteryCapturePage : ContentPage
{
    private volatile bool disposed;
    private CancellationTokenSource? pulseCts;
    private LotteryCaptureViewModel? vm;

    public LotteryCapturePage(LotteryCaptureViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        disposed = false;

        if (BindingContext is LotteryCaptureViewModel viewModel)
        {
            vm = viewModel;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            await vm.InitializeAsync();
            RestartCapturePulse();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        disposed = true;
        if (vm != null)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Cleanup();
        }

        StopCapturePulse();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LotteryCaptureViewModel.HasImage)
            or nameof(LotteryCaptureViewModel.IsAnalyzing)
            or nameof(LotteryCaptureViewModel.ShowPreviewModal))
        {
            RestartCapturePulse();
        }
    }

    private void RestartCapturePulse()
    {
        StopCapturePulse();
        if (vm == null || disposed || CaptureFrame == null) return;
        if (vm.ShowPreviewModal || vm.HasImage || vm.IsAnalyzing) return;
        Dispatcher.Dispatch(TryStartCapturePulse);
    }

    private async void TryStartCapturePulse()
    {
        if (disposed || vm == null || CaptureFrame == null) return;

        pulseCts?.Cancel();
        pulseCts = new CancellationTokenSource();
        var token = pulseCts.Token;

        try
        {
            await RunCapturePulseAsync(token);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private async Task RunCapturePulseAsync(CancellationToken token)
    {
        if (disposed || vm == null) return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!disposed && CaptureFrame != null) CaptureFrame.Scale = 1.0;
        });

        while (!token.IsCancellationRequested && !disposed && vm != null
               && !vm.ShowPreviewModal && !vm.HasImage && !vm.IsAnalyzing)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (disposed || CaptureFrame == null) return;
                await CaptureFrame.ScaleTo(1.015, 900, Easing.CubicInOut);
                if (disposed || CaptureFrame == null) return;
                await CaptureFrame.ScaleTo(1.0, 900, Easing.CubicInOut);
            });

            if (disposed || token.IsCancellationRequested) break;
            try
            {
                await Task.Delay(250, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (!disposed)
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!disposed && CaptureFrame != null) CaptureFrame.Scale = 1.0;
            });
    }

    private void StopCapturePulse()
    {
        if (pulseCts != null)
        {
            pulseCts.Cancel();
            pulseCts.Dispose();
            pulseCts = null;
        }

        if (CaptureFrame != null)
            CaptureFrame.Scale = 1.0;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        // If preview modal is showing, dismiss it first instead of navigating back.
        if (vm?.ShowPreviewModal == true)
        {
            vm.ShowPreviewModal = false;
            return;
        }

        await NavigationService.Default.NavigateBackAsync();
    }
}
