using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
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
            
            // Khởi chạy vòng lặp thở của khung chụp ảnh
            RestartCapturePulse();

            // Khởi chạy vòng lặp hoạt ảnh quét tia laser lấp lánh
            _ = StartLaserScannerAnimation();

            // Khởi chạy vòng lặp thở (Pulse) mời gọi bấm cho nút dò vé dưới cùng
            _ = StartAnalyzeButtonPulseLoop();
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

        try
        {
            pulseCts?.Cancel();
            pulseCts?.Dispose();
            pulseCts = new CancellationTokenSource();
            var token = pulseCts.Token;

            await RunCapturePulseAsync(token);
        }
        catch (Exception ex)
        {
            // Log but don't crash the app
            Microsoft.Maui.Controls.Internals.Log.Warning("LotteryCapture", $"TryStartCapturePulse failed: {ex.Message}");
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
        try
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
        catch
        {
            // ignored - usually happens during page teardown
        }
    }

    private async Task StartLaserScannerAnimation()
    {
        try
        {
            // Vòng lặp quét tịnh tiến tia laser lên xuống liên tục khi rảnh rỗi hoặc phân tích
            while (!disposed && this.Handler != null && LaserLine != null)
            {
                // Quét từ trên đỉnh (Y=0) xuống dưới đáy khung hình (Y=316)
                await LaserLine.TranslateTo(0, 316, 2200, Easing.SinInOut);

                if (disposed || this.Handler == null || LaserLine == null) break;

                // Quét ngược lại từ đáy lên đỉnh
                await LaserLine.TranslateTo(0, 0, 2200, Easing.SinInOut);
            }

            // Đưa tia laser về vị trí ban đầu
            if (LaserLine != null)
            {
                _ = LaserLine.TranslateTo(0, 0, 100);
            }
        }
        catch (Exception ex)
        {
            Microsoft.Maui.Controls.Internals.Log.Warning("LotteryCapture", $"Laser Animation failed: {ex.Message}");
        }
    }

    private async Task StartAnalyzeButtonPulseLoop()
    {
        try
        {
            // Vòng lặp co giãn nút bấm chính dưới cùng nhằm kích thích thị giác
            while (!disposed && this.Handler != null && AnalyzeButton != null)
            {
                _ = AnalyzeButton.ScaleTo(1.03, 1000, Easing.SinInOut);
                await Task.Delay(1000);

                if (disposed || this.Handler == null || AnalyzeButton == null) break;

                _ = AnalyzeButton.ScaleTo(0.97, 1000, Easing.SinInOut);
                await Task.Delay(1000);
            }

            // Đưa nút về trạng thái cân bằng ban đầu
            if (AnalyzeButton != null)
            {
                _ = AnalyzeButton.ScaleTo(1.0, 100);
            }
        }
        catch (Exception ex)
        {
            Microsoft.Maui.Controls.Internals.Log.Warning("LotteryCapture", $"Button Pulse failed: {ex.Message}");
        }
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
