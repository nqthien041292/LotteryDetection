using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Voice;
using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Logging;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.Services.Voice;
using Microsoft.AspNetCore.SignalR.Client;

namespace LotteryDetectionMobile.ViewModel;

public class LotteryCaptureViewModel : BaseViewModel
{
    private const string DefaultTranscriptHint = "Tap record to start talking about a task.";
    private readonly IAIService aiService;

    private readonly IAuthService authService;
    private readonly HybridVoiceService? hybridVoiceService;
    private readonly Random random = new();
    private readonly IVoiceService voiceService;

    private string aiPreview = string.Empty;
    private string confirmedTranscript = string.Empty; // Final confirmed text
    private string currentInterim = string.Empty; // Current interim (partial) text
    private double currentAmplitude; // Smoothed RMS amplitude from mic (0..1)
    private Guid? currentSessionId;
    private Guid? currentVoiceTaskId;
    private bool disposed;
    private string? extractedIntent;
    private double intentConfidence;
    private bool isConnected;
    private bool isProcessing;
    private bool isRecording;
    private bool isStartingRecording;
    private bool isStreamingRealtime;
    private PermissionStatus micPermissionStatus = PermissionStatus.Unknown;
    private string recordingDuration = "00:00";
    private int recordingSeconds;
    private IDispatcherTimer? recordingTimer;
    private bool showPreviewModal;
    private string statusMessage = "Idle";
    private CancellationTokenSource? streamCts;
    private string transcript = DefaultTranscriptHint;
    private string? uploadedFilePath;
    private IDispatcherTimer? waveformTimer;
    private VoiceTaskPreview? previewData;

    public LotteryCaptureViewModel(IAuthService authService, IVoiceService voiceService, IAIService aiService)
    {
        this.authService = authService;
        this.voiceService = voiceService;
        this.aiService = aiService;
        ToggleRecordCommand = new Command(async () => await ToggleRecordAsync(), () => !IsProcessing && !IsStartingRecording);
        SaveTaskCommand = new Command(async () => await SaveTaskAsync(), () => CanSave);
        NewRecordCommand = new Command(StartNewRecording);
        RequestPermissionCommand = new Command(async () => await RefreshPermissionAsync(true));
        ResetWaveform();

        // Subscribe to SignalR events if using HybridVoiceService
        if (voiceService is HybridVoiceService hvs)
        {
            hybridVoiceService = hvs;
            hybridVoiceService.OnStatusChanged += HandleSignalRStatusChanged;
            hybridVoiceService.OnIntentExtracted += HandleSignalRIntentExtracted;
            hybridVoiceService.OnTaskCreated += HandleSignalRTaskCreated;
            hybridVoiceService.OnError += HandleSignalRError;
            hybridVoiceService.OnConnectionStateChanged += HandleSignalRConnectionStateChanged;
            hybridVoiceService.OnStreamingFallback += HandleStreamingFallback;
            hybridVoiceService.OnCriticalError += HandleCriticalError;
            hybridVoiceService.OnAmplitudeChanged += HandleAmplitudeChanged;
        }

    }

    private void HandleAmplitudeChanged(double amp)
    {
        // Fast attack (latch to peak), slow decay so bars don't flicker.
        currentAmplitude = Math.Max(amp, currentAmplitude * 0.92);
    }

    private const int WaveformBarCount = 12;
    private const double WaveformIdleHeight = 4;

    public ObservableCollection<WaveformBar> LeftWaveform { get; } = new();
    public ObservableCollection<WaveformBar> RightWaveform { get; } = new();

    public string Transcript
    {
        get => transcript;
        set
        {
            if (SetProperty(ref transcript, value))
            {
                NotifyPropertyChanged(nameof(CanSave));
                RefreshCommands();
            }
        }
    }

    public string AiPreview
    {
        get => aiPreview;
        set
        {
            if (SetProperty(ref aiPreview, value))
                NotifyPropertyChanged(nameof(ShowPreview));
        }
    }

    public VoiceTaskPreview? PreviewData
    {
        get => previewData;
        set => SetProperty(ref previewData, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public bool IsRecording
    {
        get => isRecording;
        set
        {
            RemoteLogService.Instance.Debug("LotteryCapture", $"IsRecording: {isRecording} → {value}");
            if (SetProperty(ref isRecording, value))
            {
                NotifyPropertyChanged(nameof(IsBusy));
                NotifyPropertyChanged(nameof(RecordButtonText));
                NotifyPropertyChanged(nameof(CanSave));
                NotifyPropertyChanged(nameof(ShowPreview));
                NotifyPropertyChanged(nameof(ShowCaptureArea));
                NotifyPropertyChanged(nameof(ShowIdleMic));
                NotifyPropertyChanged(nameof(StateLabel));
                if (value) StartRecordingTimer();
                else StopRecordingTimer();
                RefreshCommands();
            }
        }
    }

    public bool IsProcessing
    {
        get => isProcessing;
        set
        {
            RemoteLogService.Instance.Debug("LotteryCapture", $"IsProcessing: {isProcessing} → {value}");
            if (SetProperty(ref isProcessing, value))
            {
                NotifyPropertyChanged(nameof(IsBusy));
                NotifyPropertyChanged(nameof(CanSave));
                NotifyPropertyChanged(nameof(ShowPreview));
                NotifyPropertyChanged(nameof(ShowCaptureArea));
                NotifyPropertyChanged(nameof(ShowIdleMic));
                NotifyPropertyChanged(nameof(ShowRecordButton));
                NotifyPropertyChanged(nameof(ShowLoadingIndicator));
                NotifyPropertyChanged(nameof(LoadingIndicatorText));
                NotifyPropertyChanged(nameof(StateLabel));
                RefreshCommands();
            }
        }
    }

    public bool IsStartingRecording
    {
        get => isStartingRecording;
        set
        {
            RemoteLogService.Instance.Debug("LotteryCapture", $"IsStartingRecording: {isStartingRecording} → {value}");
            if (SetProperty(ref isStartingRecording, value))
            {
                NotifyPropertyChanged(nameof(IsBusy));
                NotifyPropertyChanged(nameof(RecordButtonText));
                NotifyPropertyChanged(nameof(CanSave));
                NotifyPropertyChanged(nameof(ShowPreview));
                NotifyPropertyChanged(nameof(ShowCaptureArea));
                NotifyPropertyChanged(nameof(ShowIdleMic));
                NotifyPropertyChanged(nameof(ShowRecordButton));
                NotifyPropertyChanged(nameof(ShowLoadingIndicator));
                NotifyPropertyChanged(nameof(LoadingIndicatorText));
                NotifyPropertyChanged(nameof(StateLabel));
                RefreshCommands();
            }
        }
    }

    public new bool IsBusy => IsRecording || IsProcessing || IsStartingRecording;

    public string RecordButtonText => IsRecording ? "Stop" : "Record";

    public bool ShowPreview =>
        !IsRecording && !IsProcessing && !IsStartingRecording && !string.IsNullOrWhiteSpace(AiPreview);

    public bool ShowPermissionNotice => micPermissionStatus != PermissionStatus.Granted;

    public bool ShowCaptureArea => IsRecording || IsProcessing || IsStartingRecording;

    public bool ShowIdleMic => !IsRecording && !IsProcessing && !IsStartingRecording;

    public bool ShowRecordButton => !IsProcessing && !IsStartingRecording;

    public bool ShowLoadingIndicator => IsProcessing || IsStartingRecording;

    public string LoadingIndicatorText =>
        IsStartingRecording ? "Starting recording..." :
        IsProcessing ? "AI is thinking..." :
        string.Empty;

    /// <summary>Elapsed recording time displayed as MM:SS.</summary>
    public string RecordingDuration
    {
        get => recordingDuration;
        private set => SetProperty(ref recordingDuration, value);
    }

    /// <summary>Dynamic state label: Ready / Starting / Recording / Processing.</summary>
    public string StateLabel =>
        IsStartingRecording ? "Starting..." :
        IsRecording ? "Recording..." :
        IsProcessing ? "Processing..." :
        "Ready to record";

    public PermissionStatus MicPermissionStatus
    {
        get => micPermissionStatus;
        private set
        {
            if (SetProperty(ref micPermissionStatus, value)) NotifyPropertyChanged(nameof(ShowPermissionNotice));
        }
    }

    public bool CanSave =>
        !IsRecording && !IsProcessing && !IsStartingRecording && HasUserTranscript(Transcript);

    public string? ExtractedIntent
    {
        get => extractedIntent;
        set => SetProperty(ref extractedIntent, value);
    }

    public double IntentConfidence
    {
        get => intentConfidence;
        set => SetProperty(ref intentConfidence, value);
    }

    public bool IsConnected
    {
        get => isConnected;
        private set => SetProperty(ref isConnected, value);
    }

    public bool IsStreamingRealtime
    {
        get => isStreamingRealtime;
        private set => SetProperty(ref isStreamingRealtime, value);
    }

    public ICommand ToggleRecordCommand { get; }

    public ICommand SaveTaskCommand { get; }

    public ICommand NewRecordCommand { get; }

    public ICommand RequestPermissionCommand { get; }

    public bool ShowPreviewModal
    {
        get => showPreviewModal;
        set => SetProperty(ref showPreviewModal, value);
    }

    /// <summary>
    ///     Checks if a transcript string contains real user content (not empty, whitespace, or default hint).
    /// </summary>
    private static bool HasUserTranscript(string? text)
    {
        return !string.IsNullOrWhiteSpace(text)
               && !string.Equals(text?.Trim(), DefaultTranscriptHint, StringComparison.Ordinal);
    }

    public async Task InitializeAsync()
    {
        ResetWaveform();
        await RefreshPermissionAsync(false);
    }

    private async Task ToggleRecordAsync()
    {
        var log = RemoteLogService.Instance;
        log.Info("LotteryCapture", $"ToggleRecordAsync called. IsRecording={IsRecording}");

        if (!await EnsureMicPermissionForRecordAsync())
        {
            log.Warn("LotteryCapture", "Microphone permission not granted");
            return;
        }

        if (IsRecording)
        {
            log.Info("LotteryCapture", "Stopping recording...");
            // Cancel streaming first
            streamCts?.Cancel();

            // Set recording to false and processing to true atomically to avoid Record button flash
            IsRecording = false;
            IsProcessing = true;
            StatusMessage = "Uploading audio...";

            // Wait a moment for cancellation to propagate
            await Task.Delay(100);

            // Now stop the recording safely
            try
            {
                log.Info("LotteryCapture", "Calling voiceService.StopRecordingAsync...");
                await voiceService.StopRecordingAsync();
                log.Info("LotteryCapture", $"Recording stopped. LastRecordingPath: {voiceService.LastRecordingPath}");
            }
            catch (Exception ex)
            {
                log.Error("LotteryCapture", $"Error stopping recording: {ex.Message}", ex);
                // Continue processing even if stop fails
            }
            finally
            {
                // Clean up cancellation token
                streamCts?.Dispose();
                streamCts = null;
            }
            ResetWaveform();

            try
            {
                log.Info("LotteryCapture", "Starting upload and preview generation");
                var uploadSucceeded = await UploadRecordingAsync();
                log.Info("LotteryCapture", $"Upload result: {uploadSucceeded}");

                // Snapshot transcript state before async operations (may be mutated by SignalR concurrently)
                var capturedTranscript = Transcript;
                var hasTranscript = HasUserTranscript(capturedTranscript);

                // Poll backend for AI-extracted preview (entities)
                if (uploadSucceeded && currentVoiceTaskId != null)
                {
                    StatusMessage = "Analyzing your request with AI...";
                    log.Info("LotteryCapture", $"Polling for preview, voiceTaskId={currentVoiceTaskId.Value}");
                    var preview = await PollForPreviewAsync(currentVoiceTaskId.Value);
                    log.Info("LotteryCapture",
                        $"Poll result: preview={preview != null}, title={preview?.Title}, assignee={preview?.Assignee}, status={preview?.Status}, transcript={preview?.Transcript?.Length ?? 0} chars");

                    // Empty-task guard: refuse to surface a task when there's no audible content
                    if (preview != null
                        && string.IsNullOrWhiteSpace(preview.Transcript)
                        && string.IsNullOrWhiteSpace(preview.Title))
                    {
                        log.Warn("LotteryCapture", "Empty transcript + empty title — refusing to create task");
                        StatusMessage = "We didn't catch any audio. Please try again.";
                        AiPreview = string.Empty;
                        ShowPreviewModal = false;

                        await AppDialog.ShowAlertAsync(
                            title: "No Audio Captured",
                            message: "We couldn't hear what you said. Please check your microphone and try again.");
                        return;
                    }

                    if (preview != null)
                    {
                        var formatted = FormatPreview(preview);
                        log.Info("LotteryCapture", $"FormatPreview result: [{formatted}]");
                        AiPreview = formatted;
                        PreviewData = preview;

                        if (!string.IsNullOrEmpty(preview.Transcript))
                            Transcript = preview.Transcript;
                    }
                    else if (hasTranscript)
                    {
                        log.Warn("LotteryCapture", "Preview null after polling, using live transcript as fallback");
                        AiPreview = $"Task: {capturedTranscript}";
                    }
                    else
                    {
                        log.Warn("LotteryCapture", "Preview null and no transcript - backend processing may have failed");
                        AiPreview = "Task: (processing incomplete - please try again)";
                    }

                    StatusMessage = BuildReadyStatusMessage();
                    // Guard: user may have discarded while upload/poll was in flight.
                    if (currentSessionId != null)
                        ShowPreviewModal = true;
                }
                else if (hasTranscript)
                {
                    // Upload failed but we have a live transcript - show it as fallback
                    log.Warn("LotteryCapture", "Upload failed but live transcript available, showing fallback preview");
                    AiPreview =
                        $"Task: {capturedTranscript}\nOwner: Unassigned\n\n(Upload failed - AI analysis unavailable)";
                    StatusMessage = "Audio upload failed. You can still save using the live transcript.";
                    if (currentSessionId != null)
                        ShowPreviewModal = true;
                }
                else
                {
                    // Nothing to show - upload failed and no live transcript
                    log.Error("LotteryCapture", "Upload failed and no transcript captured");
                    StatusMessage = "Recording failed. Please check your connection and try again.";

                    await AppDialog.ShowAlertAsync(
                        title: "Upload Failed",
                        message: "Could not upload the recording to the server. Please check your internet connection and try again.");
                }

                log.Info("LotteryCapture",
                    $"AI Preview generated: {!string.IsNullOrEmpty(AiPreview)}, ShowPreviewModal: {ShowPreviewModal}");
            }
            catch (TaskCanceledException tex)
            {
                log.Error("LotteryCapture", "Upload/preview timed out", tex);
                StatusMessage = "Request timed out. Please check your connection and try again.";
                AiPreview = string.Empty;

                await AppDialog.ShowAlertAsync(
                    title: "Timeout",
                    message: "The request timed out. Please check your connection and try again.");
            }
            catch (Exception ex)
            {
                log.Error("LotteryCapture", $"Error during upload/preview: {ex.Message}", ex);
                StatusMessage = "Processing failed. You can try recording again.";
                AiPreview = string.Empty;

                await AppDialog.ShowAlertAsync(
                    title: "Error",
                    message: $"Failed to process recording: {ex.Message}");
            }
            finally
            {
                log.Info("LotteryCapture", "Processing complete, resetting state");
                IsProcessing = false;
                ResetWaveform();
            }

            return;
        }

        log.Info("LotteryCapture", "Starting new recording...");

        // Ensure user is authenticated before starting (required for API + SignalR)
        if (!await EnsureAuthForRecordAsync())
        {
            log.Warn("LotteryCapture", "Authentication not available, aborting recording");
            return;
        }

        Transcript = string.Empty;
        confirmedTranscript = string.Empty;
        currentInterim = string.Empty;
        AiPreview = string.Empty;
        PreviewData = null;
        StatusMessage = "Starting recording...";
        currentSessionId = null;
        uploadedFilePath = null;
        IsStreamingRealtime = false;
        IsStartingRecording = true;
        StartWaveformAnimation();

        streamCts?.Cancel();
        streamCts = new CancellationTokenSource();

        // Start session first to get sessionId (needed for streaming)
        try
        {
            log.Info("LotteryCapture", "Calling voiceService.StartAsync to create session...");
            currentSessionId = await voiceService.StartAsync(streamCts.Token);
            log.Info("LotteryCapture", $"Session created: {currentSessionId}");
        }
        catch (Exception ex)
        {
            log.Error("LotteryCapture", $"Failed to start session: {ex.Message}", ex);
            // Continue with local recording even if session fails
            StatusMessage = "Server unavailable. Recording locally...";
        }

        // Now start recording (streaming uses sessionId from above)
        try
        {
            log.Info("LotteryCapture", "Calling voiceService.StartRecordingAsync...");
            await voiceService.StartRecordingAsync(streamCts.Token);
            log.Info("LotteryCapture", "Recording started successfully");
        }
        catch (Exception ex)
        {
            log.Error("LotteryCapture", $"Failed to start recording: {ex.Message}", ex);
            StatusMessage = "Failed to start recording. Please check microphone access.";
            IsStartingRecording = false;
            ResetWaveform();
            streamCts?.Dispose();
            streamCts = null;

            await AppDialog.ShowAlertAsync(
                title: "Recording Error",
                message: "Unable to start audio recording. Please ensure microphone permission is granted and try again.");
            return;
        }

        IsRecording = true;
        IsStartingRecording = false;

        // Check if real-time streaming is active (set by StartRecordingAsync)
        IsStreamingRealtime = hybridVoiceService?.IsStreamingRealtime ?? false;
        log.Info("LotteryCapture", $"Recording active. IsStreamingRealtime={IsStreamingRealtime}");
        StatusMessage = IsStreamingRealtime
            ? "Listening... (live transcription)"
            : "Listening... (will transcribe after stop)";

        StartWaveformAnimation();
        _ = ConsumeTranscriptAsync(streamCts.Token);
    }

    private async Task ConsumeTranscriptAsync(CancellationToken token)
    {
        await foreach (var (text, isFinal) in voiceService.StreamTranscriptAsync(token))
        {
            if (token.IsCancellationRequested) break;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (isFinal)
                {
                    // Final result: append to confirmed transcript and clear interim
                    confirmedTranscript = string.IsNullOrWhiteSpace(confirmedTranscript)
                        ? text
                        : $"{confirmedTranscript} {text}";
                    currentInterim = string.Empty;
                }
                else
                {
                    // Interim result: replace current interim (partial recognition)
                    currentInterim = text;
                }

                // Display confirmed + current interim
                Transcript = string.IsNullOrWhiteSpace(currentInterim)
                    ? confirmedTranscript
                    : $"{confirmedTranscript} {currentInterim}".Trim();

                if (string.IsNullOrWhiteSpace(Transcript)) Transcript = DefaultTranscriptHint;

                StatusMessage = isFinal ? "Sentence confirmed" : "Listening...";
                StartWaveformAnimation();
            });
        }
    }

    private async Task<bool> UploadRecordingAsync()
    {
        var log = RemoteLogService.Instance;
        var audioPath = voiceService.LastRecordingPath;

        log.Info("LotteryCapture", $"UploadRecordingAsync - audioPath: {audioPath}");

        if (string.IsNullOrWhiteSpace(audioPath))
        {
            log.Warn("LotteryCapture", "UploadRecordingAsync - audioPath is null/empty");
            StatusMessage = "No audio captured to upload.";
            return false;
        }

        if (!File.Exists(audioPath))
        {
            log.Warn("LotteryCapture", $"UploadRecordingAsync - file does not exist: {audioPath}");
            StatusMessage = "No audio captured to upload.";
            return false;
        }

        var fileInfo = new FileInfo(audioPath);
        log.Info("LotteryCapture", $"Audio file exists, size: {fileInfo.Length} bytes");

        if (fileInfo.Length == 0)
        {
            log.Warn("LotteryCapture", "Audio file is 0 bytes — recording was interrupted or not captured");
            StatusMessage = "No audio captured to upload.";
            return false;
        }

        try
        {
            if (currentSessionId == null)
            {
                log.Info("LotteryCapture", "No sessionId, creating new session...");
                currentSessionId = await voiceService.StartAsync(CancellationToken.None);
                log.Info("LotteryCapture", $"Session created: {currentSessionId}");
            }
            else
            {
                log.Info("LotteryCapture", $"Using existing sessionId: {currentSessionId}");
            }

            var contentType = GetContentType(audioPath);
            log.Info("LotteryCapture", $"Uploading chunk with contentType: {contentType}");

            await using var stream = File.OpenRead(audioPath);
            await voiceService.UploadChunkAsync(currentSessionId.Value, stream, contentType, CancellationToken.None);
            log.Info("LotteryCapture", "Chunk uploaded successfully");

            log.Info("LotteryCapture", "Calling CompleteAsync...");
            uploadedFilePath = await voiceService.CompleteAsync(currentSessionId.Value, CancellationToken.None);
            currentVoiceTaskId = voiceService.LastVoiceTaskId;
            log.Info("LotteryCapture",
                $"Upload complete! uploadedFilePath: {uploadedFilePath}, voiceTaskId: {currentVoiceTaskId}");

            StatusMessage = BuildReadyStatusMessage();
            return true;
        }
        catch (HttpRequestException ex)
        {
            log.Error("LotteryCapture", $"UploadRecordingAsync network error: {ex.Message} (StatusCode={ex.StatusCode})",
                ex);
            currentVoiceTaskId = null;
            StatusMessage = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Authentication expired. Please sign in again.",
                HttpStatusCode.Forbidden => "Access denied. Please sign in again.",
                _ => "Server unreachable. Please check your connection."
            };
            return false;
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            log.Error("LotteryCapture", "UploadRecordingAsync timed out", ex);
            currentVoiceTaskId = null;
            StatusMessage = "Upload timed out. Please check your connection.";
            return false;
        }
        catch (Exception ex)
        {
            log.Error("LotteryCapture", $"UploadRecordingAsync failed: {ex.Message}", ex);
            currentVoiceTaskId = null;
            StatusMessage = "Upload failed. Please try again.";
            return false;
        }
    }

    private async Task<bool> EnsureMicPermissionForRecordAsync()
    {
        if (MicPermissionStatus == PermissionStatus.Granted)
            return true;

        await RefreshPermissionAsync(true);
        if (MicPermissionStatus == PermissionStatus.Granted)
            return true;

        StatusMessage = "Microphone permission needed to record.";

        var open = await AppDialog.ShowConfirmAsync(
            title: "Microphone required",
            message: "Please enable microphone access in Settings to record voice tasks.",
            acceptText: "Open Settings",
            cancelText: "Not now",
            icon: "🎙",
            iconBackground: "#DBEAFE");
        if (open) AppInfo.ShowSettingsUI();

        return false;
    }

    /// <summary>
    ///     Ensures the user is authenticated before recording.
    ///     Tries silent token acquisition first, then prompts sign-in if needed.
    /// </summary>
    private async Task<bool> EnsureAuthForRecordAsync()
    {
        var log = RemoteLogService.Instance;

        try
        {
            var token = await authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
                return true;
        }
        catch (InvalidOperationException)
        {
            // "User not signed in" - expected when no cached session
            log.Info("LotteryCapture", "No auth session, attempting sign-in");
        }
        catch (Exception ex)
        {
            log.Warn("LotteryCapture", $"Token acquisition failed: {ex.Message}");
        }

        // Try interactive sign-in
        try
        {
            var token = await authService.SignInAsync();
            if (!string.IsNullOrEmpty(token))
                return true;
        }
        catch (Exception ex)
        {
            log.Error("LotteryCapture", $"Sign-in failed: {ex.Message}", ex);
        }

        StatusMessage = "Please sign in to record voice tasks.";
        await AppDialog.ShowAlertAsync(
            title: "Sign In Required",
            message: "You need to sign in with your Microsoft account to record and upload voice tasks.");

        return false;
    }

    private async Task RefreshPermissionAsync(bool request)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (status != PermissionStatus.Granted && request)
            status = await Permissions.RequestAsync<Permissions.Microphone>();

        MicPermissionStatus = status;
    }

    private async Task SaveTaskAsync()
    {
        var log = RemoteLogService.Instance;
        var taskId = voiceService.LastVoiceTaskId;

        if (taskId == null)
        {
            log.Warn("LotteryCapture", "SaveTaskAsync - No VoiceTaskId available");
            await AppDialog.ShowAlertAsync(
                title: "Upload Required",
                message: "The audio upload didn't complete. Please record again.");
            return;
        }

        log.Info("LotteryCapture", $"SaveTaskAsync - Confirming task {taskId}");
        IsProcessing = true;
        StatusMessage = "Creating task in Microsoft 365...";

        try
        {
            var result = await voiceService.ConfirmTaskAsync(taskId.Value, CancellationToken.None);
            log.Info("LotteryCapture", $"SaveTaskAsync - Task confirmed: {result.Status} - {result.Message}");

            if (string.Equals(result.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                log.Warn("LotteryCapture", $"SaveTaskAsync - Task creation failed: {result.Message}");
                await AppDialog.ShowAlertAsync(
                    title: "Task creation failed",
                    message: string.IsNullOrEmpty(result.Message) ? "Please try again." : result.Message);
                return;
            }

            // Close modal before navigating
            ShowPreviewModal = false;

            // Navigate to My Tasks via absolute tab routing so back returns to Dashboard,
            // not to LotteryCapturePage (which is done/irrelevant after task creation).
            log.Info("LotteryCapture", "Navigating to MyTasksPage after successful save");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await NavigationService.Default.NavigateToRootTabAsync("task");
            });

            // Reset state after navigation so LotteryCapturePage is clean when user returns
            StartNewRecording();
        }
        catch (Exception ex)
        {
            log.Error("LotteryCapture", $"SaveTaskAsync failed: {ex.Message}", ex);
            StatusMessage = "Failed to create task. Please try again.";

            await AppDialog.ShowAlertAsync(
                title: "Error",
                message: $"Failed to create task: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            ResetWaveform();
        }
    }

    /// <summary>
    ///     Polls the backend preview endpoint until transcription + entity extraction completes.
    ///     Waits for entities (Title) to be populated, not just TranscriptionCompleted status.
    /// </summary>
    private async Task<VoiceTaskPreview?> PollForPreviewAsync(Guid taskId)
    {
        var log = RemoteLogService.Instance;
        const int maxAttempts = 20; // 40 seconds at 2s intervals
        const int delayMs = 2000;
        VoiceTaskPreview? lastPreview = null;

        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                var preview = await voiceService.GetPreviewAsync(taskId, CancellationToken.None);
                if (preview == null)
                {
                    log.Debug("LotteryCapture", $"PollForPreview attempt {i + 1}: null response");
                    await Task.Delay(delayMs);
                    continue;
                }

                lastPreview = preview;
                log.Info("LotteryCapture",
                    $"PollForPreview attempt {i + 1}: status={preview.Status}, title={preview.Title ?? "(null)"}, assignee={preview.Assignee ?? "(null)"}");

                // If failed, return what we have
                if (preview.Status == "Failed")
                {
                    log.Warn("LotteryCapture", "Transcription failed on backend");
                    return preview;
                }

                // Best case: transcription completed AND entities populated
                if (preview.Status == "TranscriptionCompleted" && !string.IsNullOrEmpty(preview.Title))
                    return preview;

                // Transcription done but entities still missing - keep polling a few more times
                // (backend on-demand extraction may need another request to complete)
                if (preview.Status == "TranscriptionCompleted" && string.IsNullOrEmpty(preview.Title) &&
                    i >= maxAttempts - 5)
                {
                    log.Warn("LotteryCapture",
                        "TranscriptionCompleted but entities still missing, returning what we have");
                    return preview;
                }
            }
            catch (Exception ex)
            {
                log.Warn("LotteryCapture", $"PollForPreview error: {ex.Message}");
            }

            await Task.Delay(delayMs);
        }

        log.Warn("LotteryCapture", "PollForPreview timed out, returning last preview");
        return lastPreview;
    }

    /// <summary>
    ///     Formats AI-extracted preview data for display.
    /// </summary>
    private static string FormatPreview(VoiceTaskPreview p)
    {
        var lines = new StringBuilder();

        lines.AppendLine($"Task: {p.Title ?? p.Transcript ?? "Untitled"}");
        lines.AppendLine($"Owner: {p.Assignee ?? "Unassigned"}");

        if (!string.IsNullOrEmpty(p.DueDateTime))
            lines.AppendLine($"When: {DateTimeFormatHelper.FormatDueDateTime(p.DueDateTime)}");

        if (!string.IsNullOrEmpty(p.Priority))
            lines.AppendLine($"Priority: {p.Priority}");

        if (!string.IsNullOrEmpty(p.Location))
            lines.AppendLine($"Location: {p.Location}");

        if (!string.IsNullOrEmpty(p.Notes))
            lines.AppendLine($"Notes: {p.Notes}");

        return lines.ToString().TrimEnd();
    }

    private void RefreshCommands()
    {
        (ToggleRecordCommand as Command)?.ChangeCanExecute();
        (SaveTaskCommand as Command)?.ChangeCanExecute();
    }

    private void ResetWaveform()
    {
        EnsureWaveformInitialized();
        for (var i = 0; i < LeftWaveform.Count; i++)
        {
            LeftWaveform[i].Height = WaveformIdleHeight;
            RightWaveform[i].Height = WaveformIdleHeight;
        }
    }

    private void EnsureWaveformInitialized()
    {
        if (LeftWaveform.Count == WaveformBarCount && RightWaveform.Count == WaveformBarCount)
            return;

        LeftWaveform.Clear();
        RightWaveform.Clear();
        for (var i = 0; i < WaveformBarCount; i++)
        {
            LeftWaveform.Add(new WaveformBar(WaveformIdleHeight));
            RightWaveform.Add(new WaveformBar(WaveformIdleHeight));
        }
    }

    private void StartWaveformAnimation()
    {
        if (disposed || waveformTimer?.IsRunning == true) return;

        waveformTimer ??= Application.Current?.Dispatcher?.CreateTimer();
        if (waveformTimer == null) return;

        waveformTimer.Interval = TimeSpan.FromMilliseconds(80);
        waveformTimer.Tick += OnWaveformTimerTick;
        waveformTimer.Start();
    }

    private void OnWaveformTimerTick(object? sender, EventArgs e)
    {
        if (disposed || (!IsRecording && !IsStartingRecording))
        {
            StopWaveformTimer();
            ResetWaveform();
            return;
        }

        UpdateWaveformFrame();
    }

    private void StopWaveformTimer()
    {
        if (waveformTimer != null)
        {
            waveformTimer.Stop();
            waveformTimer.Tick -= OnWaveformTimerTick;
        }
    }

    /// <summary>
    /// Stops any active recording / in-flight upload and resets all state.
    /// Called when the user taps Discard.
    /// </summary>
    public async Task DiscardAsync()
    {
        // Cancel the streaming CTS first so ConsumeTranscriptAsync exits.
        streamCts?.Cancel();

        // Stop the microphone hardware if it is still running.
        if (isRecording || isStartingRecording)
        {
            try { await voiceService.StopRecordingAsync(); }
            catch { /* best-effort */ }
        }

        StartNewRecording();
    }

    private void StartNewRecording()
    {
        StopRecordingTimer();
        RecordingDuration = "00:00";
        ShowPreviewModal = false;
        Transcript = DefaultTranscriptHint;
        confirmedTranscript = string.Empty;
        currentInterim = string.Empty;
        AiPreview = string.Empty;
        StatusMessage = "Idle";
        IsRecording = false;
        IsProcessing = false;
        IsStartingRecording = false;
        currentAmplitude = 0;
        currentSessionId = null;
        currentVoiceTaskId = null;
        uploadedFilePath = null;
        streamCts?.Cancel();
        streamCts?.Dispose();
        streamCts = null;
        NotifyPropertyChanged(nameof(ShowIdleMic));
        NotifyPropertyChanged(nameof(ShowCaptureArea));
        ResetWaveform();
    }

    private string BuildReadyStatusMessage()
    {
        if (!string.IsNullOrWhiteSpace(uploadedFilePath))
            return $"Ready to save. Audio uploaded to {uploadedFilePath}.";

        if (!string.IsNullOrWhiteSpace(voiceService.LastRecordingPath))
            return $"Ready to save. Audio saved to {voiceService.LastRecordingPath}.";

        return "Ready to save.";
    }

    private static string GetContentType(string audioPath)
    {
        var extension = Path.GetExtension(audioPath)?.ToLowerInvariant();
        return extension switch
        {
            ".wav" => "audio/wav",
            ".m4a" => "audio/m4a",
            ".aac" => "audio/aac",
            _ => "application/octet-stream"
        };
    }

    private void UpdateWaveformFrame()
    {
        EnsureWaveformInitialized();

        // Bars closer to the mic (highest index) are taller; outermost (index 0) is shortest.
        var maxHeight = IsStartingRecording ? 36.0 : 80.0;
        const double minHeight = 4.0;

        // While recording, drive bars by mic envelope. Otherwise (starting/processing) use a gentle idle level.
        // Tick-based decay keeps bars settling even between amplitude events.
        if (!IsRecording)
            currentAmplitude *= 0.85;

        var envelope = IsRecording
            ? Math.Clamp(currentAmplitude, 0.05, 1.0)
            : 0.45;

        for (var i = 0; i < LeftWaveform.Count; i++)
        {
            // Raised taper floor (0.4→1.0) so even the outermost bars are visible at normal volume.
            var taper = 0.4 + 0.6 * ((i + 1) / (double)LeftWaveform.Count);
            // Per-bar jitter so bars don't all rise/fall identically.
            var leftJitter = 0.55 + random.NextDouble() * 0.45;
            var rightJitter = 0.55 + random.NextDouble() * 0.45;
            var leftHeight = minHeight + envelope * maxHeight * taper * leftJitter;
            var rightHeight = minHeight + envelope * maxHeight * taper * rightJitter;
            LeftWaveform[i].Height = Math.Round(leftHeight);
            RightWaveform[RightWaveform.Count - 1 - i].Height = Math.Round(rightHeight);
        }
    }

    private void StartRecordingTimer()
    {
        recordingSeconds = 0;
        RecordingDuration = "00:00";
        recordingTimer = Application.Current?.Dispatcher.CreateTimer();
        if (recordingTimer == null) return;
        recordingTimer.Interval = TimeSpan.FromSeconds(1);
        recordingTimer.Tick += (_, _) =>
        {
            recordingSeconds++;
            RecordingDuration = $"{recordingSeconds / 60:D2}:{recordingSeconds % 60:D2}";
        };
        recordingTimer.Start();
    }

    private void StopRecordingTimer()
    {
        recordingTimer?.Stop();
        recordingTimer = null;
    }

    /// <summary>
    ///     Unsubscribes all event handlers and stops timers. Called from page OnDisappearing.
    /// </summary>
    public void Cleanup()
    {
        if (disposed) return;
        disposed = true;
        ShowPreviewModal = false;

        StopRecordingTimer();
        StopWaveformTimer();
        waveformTimer = null;

        streamCts?.Cancel();
        streamCts?.Dispose();
        streamCts = null;

        if (hybridVoiceService != null)
        {
            hybridVoiceService.OnStatusChanged -= HandleSignalRStatusChanged;
            hybridVoiceService.OnIntentExtracted -= HandleSignalRIntentExtracted;
            hybridVoiceService.OnTaskCreated -= HandleSignalRTaskCreated;
            hybridVoiceService.OnError -= HandleSignalRError;
            hybridVoiceService.OnConnectionStateChanged -= HandleSignalRConnectionStateChanged;
            hybridVoiceService.OnStreamingFallback -= HandleStreamingFallback;
            hybridVoiceService.OnCriticalError -= HandleCriticalError;
            hybridVoiceService.OnAmplitudeChanged -= HandleAmplitudeChanged;
        }
    }

    #region SignalR Event Handlers

    private void HandleSignalRStatusChanged(Guid sessionId, string status, string? transcript)
    {
        if (disposed || sessionId != currentSessionId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (disposed) return;
            StatusMessage = $"Status: {status}";

            if (!string.IsNullOrEmpty(transcript)) Transcript = transcript;

            if (status is "Completed" or "Failed")
            {
                IsProcessing = false;
                ResetWaveform();
            }
        });
    }

    private void HandleSignalRIntentExtracted(Guid sessionId, string intent, object entities, double confidence)
    {
        if (disposed || sessionId != currentSessionId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (disposed) return;
            ExtractedIntent = intent;
            IntentConfidence = confidence;
            StatusMessage = $"Intent: {intent} ({confidence:P0})";
        });
    }

    private void HandleSignalRTaskCreated(Guid taskId, string title, string intent)
    {
        if (disposed) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (disposed) return;
            StatusMessage = $"Task created: {title}";
            IsProcessing = false;
            ResetWaveform();
        });
    }

    private void HandleSignalRError(Guid sessionId, string errorCode, string message)
    {
        if (disposed || sessionId != currentSessionId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (disposed) return;
            StatusMessage = $"Error: {message}";
            IsProcessing = false;
            ResetWaveform();
        });
    }

    private void HandleSignalRConnectionStateChanged(HubConnectionState state)
    {
        if (disposed) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (disposed) return;
            IsConnected = state == HubConnectionState.Connected;
            RemoteLogService.Instance.Info("LotteryCapture", $"SignalR connection state: {state}");
        });
    }

    private void HandleStreamingFallback()
    {
        if (disposed) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (disposed) return;
            IsStreamingRealtime = false;
            StatusMessage = "Connection lost. Recording locally...";
            RemoteLogService.Instance.Warn("LotteryCapture",
                "Streaming fallback triggered - continuing with file recording");
        });
    }

    private void HandleCriticalError(string errorType, string message)
    {
        if (disposed) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (disposed) return;
            RemoteLogService.Instance.Error("LotteryCapture", $"Critical error: {errorType} - {message}");

            var title = errorType switch
            {
                "Simulator" => "Simulator Limitation",
                "Connection" => "Connection Error",
                "SessionStart" => "Server Error",
                _ => "Voice Error"
            };

            var displayMessage = errorType switch
            {
                "Simulator" =>
                    "Live transcription is not available on the iOS Simulator. Recording will continue and transcription will happen after upload.",
                "SessionStart" => $"Real-time transcription unavailable: {message}. Recording continues locally.",
                _ => $"An error occurred: {message}"
            };

            IsStreamingRealtime = false;
            StatusMessage = "Recording locally (no live transcription)";

            await AppDialog.ShowAlertAsync(title, displayMessage);
        });
    }

    #endregion
}
