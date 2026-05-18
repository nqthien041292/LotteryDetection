using System;

namespace LotteryDetectionMobile.Models.Dashboard;

public class DashboardMetrics
{
    public int OpenTasks { get; set; }
    public int DueToday { get; set; }
    public int Completed { get; set; }
    public DateTime UpdatedAt { get; set; }
}
