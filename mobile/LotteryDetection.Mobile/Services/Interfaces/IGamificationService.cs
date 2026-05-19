using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IGamificationService
{
    Task<int> GetCurrentPointsAsync();
    Task<int> GetWeeklyPointsAsync();
    Task<int> GetAvailablePointsAsync();
    Task<Streak> GetCurrentStreakAsync();
    Task<IEnumerable<Badge>> GetBadgesAsync();
    Task<IEnumerable<FamilyMember>> GetLeaderboardAsync();
}