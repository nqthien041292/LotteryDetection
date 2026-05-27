using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;

namespace LotteryDetection.Mobile;

internal static class AndroidCameraPermission
{
    private const int RequestCode = 7107;
    private static TaskCompletionSource<PermissionStatus>? pendingRequest;

    public static Task<PermissionStatus> CheckAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
            return Task.FromResult(PermissionStatus.Unknown);

        var granted = activity.CheckSelfPermission(global::Android.Manifest.Permission.Camera) == Permission.Granted;
        return Task.FromResult(granted ? PermissionStatus.Granted : PermissionStatus.Denied);
    }

    public static async Task<PermissionStatus> RequestAsync()
    {
        var status = await CheckAsync();
        if (status == PermissionStatus.Granted)
            return status;

        var activity = Platform.CurrentActivity;
        if (activity == null)
            return PermissionStatus.Denied;

        pendingRequest = new TaskCompletionSource<PermissionStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RequestPermissions([global::Android.Manifest.Permission.Camera], RequestCode);
        return await pendingRequest.Task;
    }

    public static bool TryHandleResult(int requestCode, Permission[] grantResults)
    {
        if (requestCode != RequestCode)
            return false;

        var status = grantResults.Length > 0 && grantResults[0] == Permission.Granted
            ? PermissionStatus.Granted
            : PermissionStatus.Denied;

        pendingRequest?.TrySetResult(status);
        pendingRequest = null;
        return true;
    }
}
