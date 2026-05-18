namespace LotteryDetectionMobile.Services.Interfaces;

public class TaskSummary
{
    public int OpenTasks { get; set; }
    public int DueToday { get; set; }
    public int Completed { get; set; }
    public List<string> Highlights { get; set; } = new();
}