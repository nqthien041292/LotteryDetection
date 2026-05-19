namespace LotteryDetection.Mobile.Services;

public interface IPermissionService
{
    Task<bool> RequestMicAndCalendarAsync();
}