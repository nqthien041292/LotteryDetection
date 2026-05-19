using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockTaskService : ITaskService
{
    public static ITaskService Instance { get; } = new MockTaskService();

    public Task<TaskSummary> GetDashboardSummaryAsync()
    {
        var summary = new TaskSummary
        {
            OpenTasks = 7,
            DueToday = 2,
            Completed = 3,
            Highlights = new List<string>
            {
                "Grocery run assigned to Alex",
                "Science project due Friday",
                "Birthday invite draft ready"
            }
        };

        return Task.FromResult(summary);
    }

    public Task<IEnumerable<TaskItem>> GetBoardTasksAsync()
    {
        // Source of truth for design seed: matches FamilyAI design bundle (4 todo / 3 doing / 3 done).
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var nextFri = today.AddDays(((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7 == 0 ? 7 : ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7);

        var tasks = new List<TaskItem>
        {
            // To do
            new()
            {
                Id = "board-1",
                Title = "Trader Joe's grocery run",
                Description = "Weekly grocery haul",
                Owner = "Alex",
                DueDate = today.AddHours(18).AddMinutes(30),
                Priority = "Medium",
                Status = "Open",
                Tags = new[] { "Errands" }
            },
            new()
            {
                Id = "board-2",
                Title = "Sign field trip form",
                Description = "Permission slip due before the trip",
                Owner = "Jordan",
                DueDate = tomorrow.AddHours(9),
                Priority = "High",
                Status = "Open",
                Tags = new[] { "School" }
            },
            new()
            {
                Id = "board-3",
                Title = "Take out recycling",
                Description = "Curbside pickup is early morning",
                Owner = string.Empty,
                DueDate = tomorrow.AddHours(7),
                Priority = "Low",
                Status = "Open",
                Tags = new[] { "Chores" }
            },
            new()
            {
                Id = "board-4",
                Title = "Refill prescription",
                Description = "Pharmacy pickup",
                Owner = "Sam",
                DueDate = nextFri.AddHours(10),
                Priority = "Medium",
                Status = "Open",
                Tags = new[] { "Health" }
            },
            // In progress
            new()
            {
                Id = "board-5",
                Title = "Pick up Sam from soccer",
                Description = "Practice ends at 5",
                Owner = "Alex",
                DueDate = today.AddHours(17),
                Priority = "High",
                Status = "InProgress",
                Tags = new[] { "Family" },
                IsPinned = true
            },
            new()
            {
                Id = "board-6",
                Title = "Vet appointment for Biscuit",
                Description = "Annual checkup",
                Owner = "Sam",
                DueDate = today.AddHours(15),
                Priority = "Medium",
                Status = "InProgress",
                Tags = new[] { "Pet" },
                IsPinned = true
            },
            new()
            {
                Id = "board-7",
                Title = "Riley's bath",
                Description = "Before bedtime",
                Owner = "Riley",
                DueDate = today.AddHours(19).AddMinutes(30),
                Priority = "Medium",
                Status = "InProgress",
                Tags = new[] { "Family" }
            },
            // Done
            new()
            {
                Id = "board-8",
                Title = "Morning carpool",
                Description = "Dropped kids at school",
                Owner = "Sam",
                DueDate = today.AddHours(8).AddMinutes(15),
                Priority = "High",
                Status = "Completed",
                Tags = new[] { "Family" }
            },
            new()
            {
                Id = "board-9",
                Title = "Pack school lunches",
                Description = "Sandwiches and fruit",
                Owner = "Alex",
                DueDate = today.AddHours(7).AddMinutes(30),
                Priority = "Medium",
                Status = "Completed",
                Tags = new[] { "Food" }
            },
            new()
            {
                Id = "board-10",
                Title = "Feed Biscuit",
                Description = "Morning kibble",
                Owner = "Jordan",
                DueDate = today.AddHours(7),
                Priority = "Low",
                Status = "Completed",
                Tags = new[] { "Pet" }
            }
        };

        return Task.FromResult(tasks.AsEnumerable());
    }

    public async Task<TaskItem?> GetTaskByIdAsync(string id)
    {
        var items = await GetBoardTasksAsync();
        return items.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public Task<IEnumerable<TaskItem>> GetRecentActivityAsync() => GetBoardActivityAsync();

    public Task<TaskItem?> CreateTaskAsync(TaskItem task)
    {
        task.Id = string.IsNullOrWhiteSpace(task.Id) ? Guid.NewGuid().ToString() : task.Id;
        return Task.FromResult<TaskItem?>(task);
    }

    public Task<TaskItem?> UpdateTaskContentAsync(TaskItem task)
    {
        return Task.FromResult<TaskItem?>(task);
    }

    public Task<TaskItem?> UpdateTaskStatusAsync(string id, string status)
    {
        return Task.FromResult<TaskItem?>(new TaskItem { Id = id, Status = status });
    }

    public Task<TaskItem?> AssignTaskAsync(string id, string assigneeId, string assigneeName)
    {
        return Task.FromResult<TaskItem?>(new TaskItem { Id = id, AssigneeId = assigneeId, Owner = assigneeName });
    }

    public Task<TaskItem?> MoveTaskAsync(string id, string columnId)
    {
        var status = columnId switch
        {
            "doing" => "InProgress",
            "done" => "Completed",
            _ => "Open"
        };
        return UpdateTaskStatusAsync(id, status);
    }

    public Task<bool> DeleteTaskAsync(string id)
    {
        return Task.FromResult(true);
    }

    public Task<IEnumerable<TaskItem>> GetBoardActivityAsync()
    {
        var items = new[]
        {
            new TaskItem
            {
                Id = "act-1",
                Title = "Shared new grocery list",
                Description = "AI summarized and added ingredients",
                Owner = "System",
                Status = "Completed",
                UpdatedAt = DateTime.Now.AddMinutes(-10)
            },
            new TaskItem
            {
                Id = "act-2",
                Title = "Task reassigned",
                Description = "Laundry rotation moved to Jordan",
                Owner = "System",
                Status = "Open",
                UpdatedAt = DateTime.Now.AddMinutes(-45)
            }
        };

        return Task.FromResult(items.AsEnumerable());
    }
}
