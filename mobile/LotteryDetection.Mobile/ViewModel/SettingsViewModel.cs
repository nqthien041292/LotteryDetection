using System.IO;
using System.Windows.Input;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Dialogs;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class SettingsViewModel : BaseViewModel
{
    // Shared key so DashboardViewModel can read the same custom avatar path.
    public const string PrefAvatarPathKey = "profile.image.path";

    private readonly INavigationService navigationService;
    private readonly IAuthService? authService;

    public SettingsViewModel()
        : this(NavigationService.Default, ResolveAuthService())
    {
    }

    public SettingsViewModel(INavigationService navigationService, IAuthService? authService)
    {
        this.navigationService = navigationService;
        this.authService = authService;

        SignOutCommand = new Command(async () => await SignOutAsync());
        UpdateAvatarCommand = new Command(async () => await UpdateAvatarAsync());
    }

    public string UserDisplayName =>
        !string.IsNullOrWhiteSpace(authService?.UserDisplayName)
            ? authService!.UserDisplayName!
            : "Khách";

    public string UserEmail =>
        !string.IsNullOrWhiteSpace(authService?.UserEmail)
            ? authService!.UserEmail!
            : "—";

    public string AppName => AppInfo.Current.Name;
    public string AppDescription => "Tự động phân tích vé số AI cho cả 3 miền Bắc — Trung — Nam.";
    public string AppVersion => $"{AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";
    public string DeviceDescription => $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} · {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";

    public string? AvatarImagePath
    {
        get
        {
            var path = Preferences.Get(PrefAvatarPathKey, null as string);
            return !string.IsNullOrEmpty(path) && File.Exists(path) ? path : null;
        }
    }

    public bool HasCustomAvatar => !string.IsNullOrEmpty(AvatarImagePath);
    public bool HasNoCustomAvatar => !HasCustomAvatar;

    public ImageSource? AvatarSource =>
        HasCustomAvatar ? ImageSource.FromFile(AvatarImagePath!) : null;

    public ICommand SignOutCommand { get; }
    public ICommand UpdateAvatarCommand { get; }

    public void RefreshAvatar()
    {
        NotifyPropertyChanged(nameof(AvatarImagePath));
        NotifyPropertyChanged(nameof(AvatarSource));
        NotifyPropertyChanged(nameof(HasCustomAvatar));
        NotifyPropertyChanged(nameof(HasNoCustomAvatar));
    }

    private async Task UpdateAvatarAsync()
    {
        FileResult? photo;
        try
        {
            photo = await MediaPicker.Default.PickPhotoAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] PickPhoto failed: {ex.Message}");
            await AppDialog.ShowAlertAsync(title: "Không mở được thư viện ảnh", message: ex.Message);
            return;
        }

        if (photo == null) return;

        byte[]? bytes = null;
        var contentType = photo.ContentType ?? "image/jpeg";
        try
        {
            var destPath = Path.Combine(FileSystem.AppDataDirectory, "profile.jpg");
            using (var src = await photo.OpenReadAsync())
            using (var ms = new MemoryStream())
            {
                await src.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            await File.WriteAllBytesAsync(destPath, bytes);
            Preferences.Set(PrefAvatarPathKey, destPath);
            RefreshAvatar();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Save avatar failed: {ex.Message}");
            await AppDialog.ShowAlertAsync(title: "Không lưu được ảnh", message: ex.Message);
            return;
        }

        // Best-effort sync to backend. Failure is non-fatal — local image is
        // already shown; the user can retry by picking another photo.
        if (bytes != null && authService != null)
        {
            try
            {
                await authService.UploadProfilePictureAsync(bytes, contentType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Upload to backend failed: {ex.Message}");
            }
        }
    }

    private async Task SignOutAsync()
    {
        var confirmed = await AppDialog.ShowConfirmAsync(
            title: "Đăng xuất",
            message: "Bạn có chắc muốn đăng xuất khỏi DòVéSố AI?",
            acceptText: "Đăng xuất",
            cancelText: "Huỷ",
            danger: true);
        if (!confirmed) return;

        try
        {
            if (authService != null)
                await authService.SignOutAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] SignOut failed: {ex.Message}");
        }

        await navigationService.NavigateToLoginWithSocialAsync();
    }

    private static IAuthService? ResolveAuthService()
        => IPlatformApplication.Current?.Services.GetService<IAuthService>();
}
