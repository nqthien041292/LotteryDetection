using LotteryDetectionMobile.Models.Family;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IFamilyMemberCache
{
    Task<IReadOnlyList<FamilyMember>> GetMembersAsync();
    void Invalidate();
}
