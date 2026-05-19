using LotteryDetectionMobile.Models.Dashboard;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Mock;

public sealed class MockDashboardRealtimeService : IDashboardRealtimeService
{
    public static IDashboardRealtimeService Instance { get; } = new MockDashboardRealtimeService();

    public event EventHandler<DashboardMetrics>? DashboardUpdated;

    public Task ConnectAsync()
    {
        DashboardUpdated?.Invoke(this, new DashboardMetrics
        {
            OpenTasks = 2,
            DueToday = 1,
            Completed = 4,
            UpdatedAt = DateTime.Now
        });
        return Task.CompletedTask;
    }

    public Task DisconnectAsync() => Task.CompletedTask;
}
