using System.Collections.Generic;

namespace LotteryDetection.Notifications.Dto;

public class UpdateNotificationSettingsInput
{
    public bool ReceiveNotifications { get; set; }

    public List<NotificationSubscriptionDto> Notifications { get; set; }
}

