namespace LotteryDetection.Mobile.Services;

public interface IPermissionService
{
    Task<PermissionStatus> CheckCameraAsync();
    Task<PermissionStatus> RequestCameraAsync();
    Task<bool> RequestMicAndCalendarAsync();
}
