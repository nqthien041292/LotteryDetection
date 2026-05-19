using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IFamilyMemberCache
{
    Task<IReadOnlyList<FamilyMember>> GetMembersAsync();
    void Invalidate();
}
