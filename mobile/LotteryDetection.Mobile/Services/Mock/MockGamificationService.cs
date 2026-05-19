using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockGamificationService : IGamificationService
{
    public static IGamificationService Instance { get; } = new MockGamificationService();

    public Task<int> GetCurrentPointsAsync() => Task.FromResult(245);

    public Task<int> GetWeeklyPointsAsync() => Task.FromResult(180);

    public Task<int> GetAvailablePointsAsync() => Task.FromResult(245);

    public Task<Streak> GetCurrentStreakAsync()
    {
        return Task.FromResult(new Streak { Label = "Consistency", Days = 5 });
    }

    public Task<IEnumerable<Badge>> GetBadgesAsync()
    {
        var badges = new[]
        {
            new Badge
            {
                Title = "Task Tamer", Description = "Completed 10 tasks in a week", Points = 50, Icon = "✅",
                IsUnlocked = true
            },
            new Badge
            {
                Title = "Early Bird", Description = "Closed 3 tasks before 8 AM", Points = 30, Icon = "🌅",
                IsUnlocked = false
            },
            new Badge
            {
                Title = "Voice Pro", Description = "Captured 5 tasks via voice", Points = 40, Icon = "🎙️",
                IsUnlocked = true
            }
        };
        return Task.FromResult(badges.AsEnumerable());
    }

    public Task<IEnumerable<FamilyMember>> GetLeaderboardAsync()
    {
        var board = new[]
        {
            new FamilyMember { Name = "Alex", Role = "Parent", Points = 245, IsOnline = true },
            new FamilyMember { Name = "Taylor", Role = "Teen", Points = 210, IsOnline = true },
            new FamilyMember { Name = "Sam", Role = "Kid", Points = 180, IsOnline = false },
            new FamilyMember { Name = "Jordan", Role = "Parent", Points = 165, IsOnline = false }
        };
        return Task.FromResult(board.AsEnumerable());
    }
}