using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Family;

public class FamilyMemberCache : IFamilyMemberCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private readonly IFamilyService _familyService;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IReadOnlyList<FamilyMember>? _cache;
    private DateTimeOffset _fetchedAt;

    public FamilyMemberCache(IFamilyService familyService)
        => _familyService = familyService;

    public async Task<IReadOnlyList<FamilyMember>> GetMembersAsync()
    {
        if (_cache != null && DateTimeOffset.UtcNow - _fetchedAt < Ttl)
            return _cache;

        await _lock.WaitAsync();
        try
        {
            if (_cache != null && DateTimeOffset.UtcNow - _fetchedAt < Ttl)
                return _cache;

            var members = await _familyService.GetMembersAsync();
            _cache = members.ToList().AsReadOnly();
            _fetchedAt = DateTimeOffset.UtcNow;
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _cache = null;
}
