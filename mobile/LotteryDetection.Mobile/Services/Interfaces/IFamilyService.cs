using System.Collections.Generic;
using System.Threading.Tasks;
using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IFamilyService
{
    Task<IEnumerable<FamilyMember>> GetMembersAsync();
    Task<FamilyGroupSummary?> GetGroupAsync();
    Task<IReadOnlyList<RoleOption>> GetRolesAsync();
    Task<FamilyMember?> InviteMemberAsync(string emailAddress, string role);
    Task<FamilyMember?> UpdateMemberRoleAsync(string memberId, string role);
    Task<bool> RemoveMemberAsync(string memberId);
    Task<bool> ResendInviteAsync(string memberId);
}
