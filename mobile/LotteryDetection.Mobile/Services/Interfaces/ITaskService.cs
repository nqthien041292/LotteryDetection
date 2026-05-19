using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface ITaskService
{
    Task<TaskSummary> GetDashboardSummaryAsync();
    Task<IEnumerable<TaskItem>> GetBoardTasksAsync();
    Task<TaskItem?> GetTaskByIdAsync(string id);
    Task<IEnumerable<TaskItem>> GetRecentActivityAsync();
    Task<IEnumerable<TaskItem>> GetBoardActivityAsync();
    Task<TaskItem?> CreateTaskAsync(TaskItem task);
    Task<TaskItem?> UpdateTaskContentAsync(TaskItem task);
    Task<TaskItem?> UpdateTaskStatusAsync(string id, string status);
    Task<TaskItem?> AssignTaskAsync(string id, string assigneeId, string assigneeName);
    Task<TaskItem?> MoveTaskAsync(string id, string columnId);
    Task<bool> DeleteTaskAsync(string id);
}
