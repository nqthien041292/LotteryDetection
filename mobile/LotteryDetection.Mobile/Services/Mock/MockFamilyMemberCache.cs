using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public sealed class MockFamilyMemberCache : IFamilyMemberCache
{
    private readonly IFamilyService familyService;

    public MockFamilyMemberCache()
        : this(MockFamilyService.Instance)
    {
    }

    public MockFamilyMemberCache(IFamilyService familyService)
    {
        this.familyService = familyService;
    }

    public static IFamilyMemberCache Instance { get; } = new MockFamilyMemberCache();

    public async Task<IReadOnlyList<FamilyMember>> GetMembersAsync()
    {
        var members = await familyService.GetMembersAsync();
        return members.ToList();
    }

    public void Invalidate()
    {
    }
}
