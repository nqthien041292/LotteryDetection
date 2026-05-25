using System.Windows.Input;
using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Dialogs;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Logging;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class LotteryCaptureViewModel : BaseViewModel
{
    private const string DefaultHint = "Chụp hoặc chọn ảnh vé để bắt đầu.";

    private readonly ILotteryDetectionService detectionService;
    private readonly INavigationService navigationService;

    private bool isCapturing;
    private bool isAnalyzing;
    private string? imagePath;
    private System.Collections.ObjectModel.ObservableCollection<LotteryTicketResult> results = new();
    private string statusHint = DefaultHint;
    private bool showPreviewModal;
    private PermissionStatus cameraPermissionStatus = PermissionStatus.Unknown;

    public LotteryCaptureViewModel(ILotteryDetectionService detectionService, INavigationService navigationService)
    {
        this.detectionService = detectionService;
        this.navigationService = navigationService;

        CaptureCommand = new Command(async () => await CaptureAsync(), () => !IsBusy);
        PickFromGalleryCommand = new Command(async () => await PickFromGalleryAsync(), () => !IsBusy);
        AnalyzeCommand = new Command(async () => await AnalyzeAsync(), () => HasImage && !IsBusy);
        RetakeCommand = new Command(Reset);
        ConfirmCommand = new Command(async () => await ConfirmAsync(), () => HasResults);
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
                NotifyPropertyChanged(nameof(CanAnalyze));
                RefreshCommands();
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath);

    public bool CanAnalyze => HasImage && !IsAnalyzing;

    public bool CanCaptureOrPick => !IsAnalyzing;

    public bool ShowEmptyState => !HasImage && !IsAnalyzing;

    public ImageSource? ImageSource => HasImage ? Microsoft.Maui.Controls.ImageSource.FromFile(ImagePath!) : null;

    public System.Collections.ObjectModel.ObservableCollection<LotteryTicketResult> Results
    {
        get => results;
        set => SetProperty(ref results, value);
    }

    public bool HasResults => Results != null && Results.Count > 0;

    public string StatusHint
    {
        get => statusHint;
        private set => SetProperty(ref statusHint, value);
    }

    public string StatusBadge =>
        IsAnalyzing ? "AI đang phân tích…" :
        HasResults ? "Đã có kết quả" :
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

        bool resizedSuccess = false;
        try
        {
            await using (var source = await photo.OpenReadAsync())
            {
                var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(source);
                if (image != null)
                {
                    // Downsize to a max dimension of 1600px (great for detail and perfect for performance)
                    using var resizedImage = image.Downsize(1600);
                    await using (var destination = File.OpenWrite(localPath))
                    {
                        resizedImage.Save(destination, Microsoft.Maui.Graphics.ImageFormat.Jpeg, 0.85f);
                    }
                    resizedSuccess = true;
                }
            }
        }
        catch (Exception ex)
        {
            RemoteLogService.Instance.Error("LotteryCapture", $"Failed to resize image, falling back to original copy: {ex.Message}", ex);
        }

        if (!resizedSuccess)
        {
            await using (var source = await photo.OpenReadAsync())
            await using (var destination = File.OpenWrite(localPath))
            {
                await source.CopyToAsync(destination);
            }
        }

        Results.Clear();
        NotifyPropertyChanged(nameof(HasResults));
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
        Results.Clear();
        NotifyPropertyChanged(nameof(HasResults));
        ShowPreviewModal = false;
        NotifyPropertyChanged(nameof(StatusBadge));

        try
        {
            var tickets = await detectionService.AnalyzeAsync(ImagePath!, CancellationToken.None);
            Results.Clear();
            foreach (var t in tickets) Results.Add(t);
            NotifyPropertyChanged(nameof(HasResults));

            var winCount = tickets.Count(t => t.IsWinner);
            if (winCount > 0)
                StatusHint = $"Chúc mừng! Tìm thấy {tickets.Count} vé, trong đó có {winCount} vé trúng.";
            else
                StatusHint = $"AI đã tìm thấy {tickets.Count} vé. Rất tiếc chưa có vé trúng giải.";
            
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

    private async Task ConfirmAsync()
    {
        // The backend already persisted the analysis row inside AnalyzeAsync,
        // so "Lưu lịch sử" is purely a UX confirmation — close the sheet, reset,
        // and jump to the history page so the user sees their new entry.
        ShowPreviewModal = false;
        Reset();
        try
        {
            await navigationService.NavigateToLotteryHistoryAsync();
        }
        catch (Exception ex)
        {
            RemoteLogService.Instance.Error("LotteryCapture", $"NavigateToHistory failed: {ex.Message}", ex);
        }
    }

    private void Reset()
    {
        ShowPreviewModal = false;
        ImagePath = null;
        Results.Clear();
        NotifyPropertyChanged(nameof(HasResults));
        StatusHint = DefaultHint;
        NotifyPropertyChanged(nameof(StatusBadge));
        NotifyPropertyChanged(nameof(CanAnalyze));
        NotifyPropertyChanged(nameof(CanCaptureOrPick));
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
        NotifyPropertyChanged(nameof(CanAnalyze));
        NotifyPropertyChanged(nameof(CanCaptureOrPick));
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
