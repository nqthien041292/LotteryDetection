using System;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Dashboard;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IDashboardRealtimeService
{
    event EventHandler<DashboardMetrics>? DashboardUpdated;
    Task ConnectAsync();
    Task DisconnectAsync();
}
