#if IOS
using EventKit;
#endif

namespace LotteryDetectionMobile.Services;

public class PermissionService : IPermissionService
{
    public static IPermissionService Default { get; } = new PermissionService();

    public async Task<bool> RequestMicAndCalendarAsync()
    {

        var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        var micGranted = micStatus == PermissionStatus.Granted;

        var calendarGranted = await RequestCalendarAsync();

        return micGranted && calendarGranted;
    }

    private static async Task<bool> RequestCalendarAsync()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            var read = await Permissions.RequestAsync<Permissions.CalendarRead>();
            var write = await Permissions.RequestAsync<Permissions.CalendarWrite>();
            return read == PermissionStatus.Granted && write == PermissionStatus.Granted;
        }

        if (DeviceInfo.Platform == DevicePlatform.iOS) return await RequestCalendarOnIosAsync();

        return true;
    }

#if IOS
    private static async Task<bool> RequestCalendarOnIosAsync()
    {
        try
        {
            var store = new EKEventStore();
            var result = await store.RequestAccessAsync(EKEntityType.Event);
            return result.Item1;
        }
        catch
        {
            return false;
        }
    }
#else
    private static Task<bool> RequestCalendarOnIosAsync()
    {
        return Task.FromResult(true);
    }
#endif
}