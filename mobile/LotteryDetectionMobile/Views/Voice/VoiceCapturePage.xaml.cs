using System.ComponentModel;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Views.Voice;

public partial class VoiceCapturePage : ContentPage
{
    private volatile bool _disposed;
    private CancellationTokenSource? micPulseCts;
    private VoiceCaptureViewModel? vm;

    public VoiceCapturePage(VoiceCaptureViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is VoiceCaptureViewModel viewModel)
        {
            vm = viewModel;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            await vm.InitializeAsync();
            RestartMicPulse();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _disposed = true;
        if (vm != null)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Cleanup();
        }

        StopMicPulse();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VoiceCaptureViewModel.IsRecording) ||
            e.PropertyName == nameof(VoiceCaptureViewModel.IsProcessing) ||
            e.PropertyName == nameof(VoiceCaptureViewModel.ShowPreviewModal))
            RestartMicPulse();

        if (e.PropertyName == nameof(VoiceCaptureViewModel.Transcript))
            ScrollTranscriptToBottom();
    }

    // Discard button (main bottom bar): stop recording + reset + navigate back.
    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        StopMicPulse();
        if (vm != null) await vm.DiscardAsync();
        await NavigationService.Default.NavigateBackAsync();
    }

    // "New recording" button inside the AI preview modal: reset without leaving the page.
    private async void OnModalNewRecordClicked(object? sender, EventArgs e)
    {
        // Command binding already executed NewRecordCommand; just restart the mic pulse.
        await Task.Delay(50);
        RestartMicPulse();
    }

    private void RestartMicPulse()
    {
        StopMicPulse();
        if (vm == null || _disposed || MicContainer == null) return;
        if (vm.ShowPreviewModal) return;
        Dispatcher.Dispatch(TryStartMicPulse);
    }

    private async void TryStartMicPulse()
    {
        if (_disposed || vm == null || MicContainer == null) return;

        micPulseCts?.Cancel();
        micPulseCts = new CancellationTokenSource();
        var token = micPulseCts.Token;

        try
        {
            await RunMicPulseAsync(token);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private async Task RunMicPulseAsync(CancellationToken token)
    {
        if (_disposed || vm == null) return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!_disposed && MicContainer != null) MicContainer.Scale = 1.0;
        });

        while (!token.IsCancellationRequested && !_disposed && vm != null && !vm.ShowPreviewModal)
        {
            // Stronger pulse when recording, gentler when idle.
            var peak = vm.IsRecording ? 1.10 : 1.06;
            var duration = vm.IsRecording ? 600u : 800u;
            var idleDelay = vm.IsRecording ? 120 : 220;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (_disposed || MicContainer == null) return;
                await MicContainer.ScaleTo(peak, duration, Easing.CubicInOut);
                if (_disposed || MicContainer == null) return;
                await MicContainer.ScaleTo(1.0, duration, Easing.CubicInOut);
            });

            if (_disposed || token.IsCancellationRequested) break;
            try
            {
                await Task.Delay(idleDelay, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (!_disposed)
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!_disposed && MicContainer != null) MicContainer.Scale = 1.0;
            });
    }

    private void StopMicPulse()
    {
        if (micPulseCts != null)
        {
            micPulseCts.Cancel();
            micPulseCts.Dispose();
            micPulseCts = null;
        }

        if (MicContainer != null)
            MicContainer.Scale = 1.0;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        // If preview modal is showing, dismiss it first instead of navigating back
        if (vm?.ShowPreviewModal == true)
        {
            vm.ShowPreviewModal = false;
            return;
        }

        await NavigationService.Default.NavigateBackAsync();
    }

    private async void ScrollTranscriptToBottom()
    {
        // The redesigned transcript card auto-grows; no parent ScrollView for the transcript any more.
        if (_disposed || LiveTranscriptLabel == null) return;
        await Task.Yield();
    }
}
