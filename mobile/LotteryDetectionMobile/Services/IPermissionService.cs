namespace LotteryDetectionMobile.Services;

public interface IPermissionService
{
    Task<bool> RequestMicAndCalendarAsync();
}