using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockNotificationService : INotificationService
{
    public static INotificationService Instance { get; } = new MockNotificationService();

    public Task<IEnumerable<NotificationItem>> GetNotificationsAsync()
    {
        var now = DateTime.Now;
        var notifications = new List<NotificationItem>
        {
            new()
            {
                Title = "Sam completed \"Vet appointment\"",
                Subtitle = "+20 XP earned · 5 min ago",
                Timestamp = now.AddMinutes(-5),
                IsUnread = true,
                Category = "Task",
                Icon = "✅",
                IconBackground = "#DCFCE7"
            },
            new()
            {
                Title = "Schedule conflict on Thursday",
                Subtitle = "Soccer pickup overlaps work meeting",
                Timestamp = now.AddMinutes(-30),
                IsUnread = true,
                Category = "Conflict",
                Icon = "⚠️",
                IconBackground = "#FEF3C7"
            },
            new()
            {
                Title = "Jordan reassigned a task to you",
                Subtitle = "\"Sign field trip form\" · 1h ago",
                Timestamp = now.AddHours(-1),
                IsUnread = true,
                Category = "Task",
                Icon = "👋",
                IconBackground = "#DBEAFE"
            },
            new()
            {
                Title = "Google Calendar synced",
                Subtitle = "12 events imported · 2h ago",
                Timestamp = now.AddHours(-2),
                IsUnread = false,
                Category = "Calendar",
                Icon = "📅",
                IconBackground = "#DBEAFE"
            },
            new()
            {
                Title = "Streak extended to 12 days!",
                Subtitle = "You're on fire · 3h ago",
                Timestamp = now.AddHours(-3),
                IsUnread = false,
                Category = "Task",
                Icon = "🔥",
                IconBackground = "#FEE2E2"
            },
            new()
            {
                Title = "Riley's bath is starting in 15m",
                Subtitle = "Reminder · 7:15 PM today",
                Timestamp = now.AddMinutes(-15),
                IsUnread = false,
                Category = "Task",
                Icon = "⏰",
                IconBackground = "#FEF3C7"
            },
            new()
            {
                Title = "New shared event: Movie night",
                Subtitle = "Sat 7PM · proposed by Sam · yesterday",
                Timestamp = now.AddDays(-1),
                IsUnread = false,
                Category = "Calendar",
                Icon = "🗓️",
                IconBackground = "#DBEAFE"
            }
        };

        return Task.FromResult(notifications.AsEnumerable());
    }

    public Task MarkAsReadAsync(string notificationId)
    {
        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync()
    {
        return Task.FromResult(3); // matches seed data above (3 IsUnread = true items)
    }
}
