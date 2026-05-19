using System;
using System.Threading.Tasks;
using LotteryDetection.Mobile.Models.Dashboard;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IDashboardRealtimeService
{
    event EventHandler<DashboardMetrics>? DashboardUpdated;
    Task ConnectAsync();
    Task DisconnectAsync();
}
