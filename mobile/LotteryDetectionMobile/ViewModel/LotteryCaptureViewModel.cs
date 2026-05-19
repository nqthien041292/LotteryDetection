using System.Windows.Input;
using LotteryDetectionMobile.Models.Lottery;
using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Logging;

namespace LotteryDetectionMobile.ViewModel;

public class LotteryCaptureViewModel : BaseViewModel
{
    private const string DefaultHint = "Chụp hoặc chọn ảnh vé để bắt đầu.";

    private readonly ILotteryDetectionService detectionService;

    private bool isCapturing;
    private bool isAnalyzing;
    private string? imagePath;
    private LotteryTicketResult? result;
    private string statusHint = DefaultHint;
    private bool showPreviewModal;
    private PermissionStatus cameraPermissionStatus = PermissionStatus.Unknown;

    public LotteryCaptureViewModel(ILotteryDetectionService detectionService)
    {
        this.detectionService = detectionService;

        CaptureCommand = new Command(async () => await CaptureAsync(), () => !IsBusy);
        PickFromGalleryCommand = new Command(async () => await PickFromGalleryAsync(), () => !IsBusy);
        AnalyzeCommand = new Command(async () => await AnalyzeAsync(), () => HasImage && !IsBusy);
        RetakeCommand = new Command(Reset);
        ConfirmCommand = new Command(async () => await ConfirmAsync(), () => Result != null);
        RequestCameraPermissionCommand = new Command(async () => await RefreshCameraPermissionAsync(true));
    }

    public bool IsCapturing
    {
        get => isCapturing;
        private set
        {
            if (SetProperty(ref isCapturing, value))
                NotifyBusyDependents();
        }
    }

    public bool IsAnalyzing
    {
        get => isAnalyzing;
        private set
        {
            if (SetProperty(ref isAnalyzing, value))
                NotifyBusyDependents();
        }
    }

    public new bool IsBusy => IsCapturing || IsAnalyzing;

    public string? ImagePath
    {
        get => imagePath;
        private set
        {
            if (SetProperty(ref imagePath, value))
            {
                NotifyPropertyChanged(nameof(HasImage));
                NotifyPropertyChanged(nameof(ShowEmptyState));
                NotifyPropertyChanged(nameof(ImageSource));
                RefreshCommands();
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath);

    public bool ShowEmptyState => !HasImage && !IsAnalyzing;

    public ImageSource? ImageSource => HasImage ? Microsoft.Maui.Controls.ImageSource.FromFile(ImagePath!) : null;

    public LotteryTicketResult? Result
    {
        get => result;
        private set
        {
            if (SetProperty(ref result, value))
            {
                NotifyPropertyChanged(nameof(HasResult));
                RefreshCommands();
            }
        }
    }

    public bool HasResult => Result != null;

    public string StatusHint
    {
        get => statusHint;
        private set => SetProperty(ref statusHint, value);
    }

    public string StatusBadge =>
        IsAnalyzing ? "AI đang phân tích…" :
        HasResult ? "Đã có kết quả" :
        HasImage ? "Ảnh đã sẵn sàng" :
        "Chạm để chụp";

    public bool ShowPreviewModal
    {
        get => showPreviewModal;
        set => SetProperty(ref showPreviewModal, value);
    }

    public PermissionStatus CameraPermissionStatus
    {
        get => cameraPermissionStatus;
        private set
        {
            if (SetProperty(ref cameraPermissionStatus, value))
                NotifyPropertyChanged(nameof(ShowPermissionNotice));
        }
    }

    public bool ShowPermissionNotice =>
        CameraPermissionStatus != PermissionStatus.Granted &&
        CameraPermissionStatus != PermissionStatus.Unknown;

    public ICommand CaptureCommand { get; }
    public ICommand PickFromGalleryCommand { get; }
    public ICommand AnalyzeCommand { get; }
    public ICommand RetakeCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand RequestCameraPermissionCommand { get; }

    public async Task InitializeAsync()
    {
        await RefreshCameraPermissionAsync(false);
    }

    public void Cleanup()
    {
        // Nothing background to dispose for the mock flow — placeholder so the
        // page can mirror VoiceCapturePage's lifecycle hooks.
    }

    private async Task CaptureAsync()
    {
        if (!await EnsureCameraPermissionAsync()) return;

        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await AppDialog.ShowAlertAsync(
                    title: "Không hỗ trợ camera",
                    message: "Thiết bị này không hỗ trợ chụp ảnh trực tiếp. Hãy chọn ảnh từ thư viện.");
                return;
            }

            IsCapturing = true;
            StatusHint = "Đang mở camera…";

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Chụp vé số"
            });

            if (photo != null)
                await LoadPhotoAsync(photo);
            else
                StatusHint = DefaultHint;
        }
        catch (Exception ex)
        {
            RemoteLogService.Instance.Error("LotteryCapture", $"CaptureAsync failed: {ex.Message}", ex);
            await AppDialog.ShowAlertAsync(title: "Không chụp được ảnh", message: ex.Message);
        }
        finally
        {
            IsCapturing = false;
        }
    }

    private async Task PickFromGalleryAsync()
    {
        try
        {
            IsCapturing = true;
            StatusHint = "Đang mở thư viện…";

            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Chọn ảnh vé số"
            });

            if (photo != null)
                await LoadPhotoAsync(photo);
            else
                StatusHint = DefaultHint;
        }
        catch (Exception ex)
        {
            RemoteLogService.Instance.Error("LotteryCapture", $"PickFromGalleryAsync failed: {ex.Message}", ex);
            await AppDialog.ShowAlertAsync(title: "Không chọn được ảnh", message: ex.Message);
        }
        finally
        {
            IsCapturing = false;
        }
    }

    private async Task LoadPhotoAsync(FileResult photo)
    {
        var extension = Path.GetExtension(photo.FileName);
        if (string.IsNullOrEmpty(extension)) extension = ".jpg";
        var fileName = $"ticket-{DateTime.Now:yyyyMMdd-HHmmss}{extension}";
        var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        await using (var source = await photo.OpenReadAsync())
        await using (var destination = File.OpenWrite(localPath))
        {
            await source.CopyToAsync(destination);
        }

        Result = null;
        ShowPreviewModal = false;
        ImagePath = localPath;
        StatusHint = "Ảnh đã sẵn sàng. Bấm \"AI dò vé số\" để phân tích.";
        NotifyPropertyChanged(nameof(StatusBadge));
    }

    private async Task AnalyzeAsync()
    {
        if (!HasImage)
        {
            await AppDialog.ShowAlertAsync(
                title: "Cần ảnh vé số",
                message: "Hãy chụp hoặc chọn ảnh vé trước khi dò.");
            return;
        }

        IsAnalyzing = true;
        StatusHint = "AI đang tách dãy số và so kết quả…";
        Result = null;
        ShowPreviewModal = false;
        NotifyPropertyChanged(nameof(StatusBadge));

        try
        {
            var ticket = await detectionService.AnalyzeAsync(ImagePath!, CancellationToken.None);
            Result = ticket;
            StatusHint = ticket.IsWinner
                ? $"Chúc mừng! Vé trúng {ticket.MatchedPrize}."
                : "AI đã phân tích xong. Vé chưa trúng giải.";
            ShowPreviewModal = true;
        }
        catch (OperationCanceledException)
        {
            StatusHint = "Đã huỷ phân tích.";
        }
        catch (Exception ex)
        {
            RemoteLogService.Instance.Error("LotteryCapture", $"AnalyzeAsync failed: {ex.Message}", ex);
            StatusHint = "Phân tích thất bại. Vui lòng thử lại.";
            await AppDialog.ShowAlertAsync(title: "Lỗi phân tích", message: ex.Message);
        }
        finally
        {
            IsAnalyzing = false;
            NotifyPropertyChanged(nameof(StatusBadge));
        }
    }

    private Task ConfirmAsync()
    {
        // No history backend yet — close the sheet and reset for the next ticket.
        ShowPreviewModal = false;
        Reset();
        return Task.CompletedTask;
    }

    private void Reset()
    {
        ShowPreviewModal = false;
        ImagePath = null;
        Result = null;
        StatusHint = DefaultHint;
        NotifyPropertyChanged(nameof(StatusBadge));
    }

    private async Task<bool> EnsureCameraPermissionAsync()
    {
        if (CameraPermissionStatus == PermissionStatus.Granted)
            return true;

        await RefreshCameraPermissionAsync(true);
        if (CameraPermissionStatus == PermissionStatus.Granted)
            return true;

        var open = await AppDialog.ShowConfirmAsync(
            title: "Cần quyền camera",
            message: "Hãy bật quyền camera trong Cài đặt để chụp ảnh vé số.",
            acceptText: "Mở cài đặt",
            cancelText: "Để sau",
            icon: "📷",
            iconBackground: "#DBEAFE");
        if (open) AppInfo.ShowSettingsUI();

        return false;
    }

    private async Task RefreshCameraPermissionAsync(bool request)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted && request)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        CameraPermissionStatus = status;
    }

    private void NotifyBusyDependents()
    {
        NotifyPropertyChanged(nameof(IsBusy));
        NotifyPropertyChanged(nameof(StatusBadge));
        NotifyPropertyChanged(nameof(ShowEmptyState));
        RefreshCommands();
    }

    private void RefreshCommands()
    {
        (CaptureCommand as Command)?.ChangeCanExecute();
        (PickFromGalleryCommand as Command)?.ChangeCanExecute();
        (AnalyzeCommand as Command)?.ChangeCanExecute();
        (ConfirmCommand as Command)?.ChangeCanExecute();
    }
}
