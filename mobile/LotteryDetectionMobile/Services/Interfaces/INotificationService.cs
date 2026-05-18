using LotteryDetectionMobile.Models.Family;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationItem>> GetNotificationsAsync();
    Task MarkAsReadAsync(string notificationId);
    Task<int> GetUnreadCountAsync();
}