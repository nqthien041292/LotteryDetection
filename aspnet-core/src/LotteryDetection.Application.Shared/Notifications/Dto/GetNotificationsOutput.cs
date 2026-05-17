using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.Notifications;

namespace LotteryDetection.Notifications.Dto;

public class GetNotificationsOutput : PagedResultDto<UserNotification>
{
    public GetNotificationsOutput(int totalCount, int unreadCount, List<UserNotification> notifications)
        : base(totalCount, notifications)
    {
        UnreadCount = unreadCount;
    }

    public int UnreadCount { get; set; }
}