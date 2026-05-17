namespace LotteryDetection.Notifications.Dto;

public class SetNotificationAsReadOutput
{
    public SetNotificationAsReadOutput(bool success)
    {
        Success = success;
    }

    public bool Success { get; set; }
}