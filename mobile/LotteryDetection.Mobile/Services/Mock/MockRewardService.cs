using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockRewardService : IRewardService
{
    public static IRewardService Instance { get; } = new MockRewardService();

    private static readonly Reward[] Catalog =
    [
        new() { Id = "screen-time", Emoji = "📱", Name = "30 min screen time",  Cost = 50  },
        new() { Id = "ice-cream",   Emoji = "🍦", Name = "Ice cream Friday",     Cost = 120 },
        new() { Id = "movie-pick",  Emoji = "🎬", Name = "Family movie pick",    Cost = 200 },
        new() { Id = "ten-dollars", Emoji = "🛍️", Name = "$10 toward anything",  Cost = 500 },
    ];

    public Task<IEnumerable<Reward>> GetRewardsAsync(int availableXp)
    {
        var result = Catalog.Select(r => new Reward
        {
            Id = r.Id,
            Emoji = r.Emoji,
            Name = r.Name,
            Cost = r.Cost,
            Affordable = availableXp >= r.Cost
        });
        return Task.FromResult(result);
    }

    public Task<RedeemResult> RedeemRewardAsync(string rewardId)
    {
        return Task.FromResult(new RedeemResult(true, 245));
    }
}
