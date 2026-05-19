using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationItem>> GetNotificationsAsync();
    Task MarkAsReadAsync(string notificationId);
    Task<int> GetUnreadCountAsync();
}