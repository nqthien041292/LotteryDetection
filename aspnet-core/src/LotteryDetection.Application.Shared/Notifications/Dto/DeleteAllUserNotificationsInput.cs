using System;
using Abp.Notifications;

namespace LotteryDetection.Notifications.Dto;

public class DeleteAllUserNotificationsInput
{
    public UserNotificationState? State { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}