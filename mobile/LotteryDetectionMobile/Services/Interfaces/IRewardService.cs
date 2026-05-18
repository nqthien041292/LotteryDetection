using LotteryDetectionMobile.Models.Family;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IRewardService
{
    Task<IEnumerable<Reward>> GetRewardsAsync(int availableXp);
    Task<RedeemResult> RedeemRewardAsync(string rewardId);
}

public record RedeemResult(bool Success, int NewAvailableXp, string? Error = null);
