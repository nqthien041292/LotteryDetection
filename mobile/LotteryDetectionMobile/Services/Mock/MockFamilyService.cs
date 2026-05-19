using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Mock;

public sealed class MockFamilyService : IFamilyService
{
    private readonly List<FamilyMember> members =
    [
        new()
        {
            Id = "demo-owner",
            Name = "Lottery Demo",
            Email = "demo@lotterydetection.local",
            Role = "Owner",
            IsOnline = true,
            IsYou = true,
            Points = 120
        },
        new()
        {
            Id = "demo-reviewer",
            Name = "Result Reviewer",
            Email = "reviewer@lotterydetection.local",
            Role = "Member",
            IsOnline = false,
            Points = 80
        }
    ];

    public static IFamilyService Instance { get; } = new MockFamilyService();

    public Task<IEnumerable<FamilyMember>> GetMembersAsync() => Task.FromResult(members.AsEnumerable());

    public Task<FamilyGroupSummary?> GetGroupAsync()
    {
        return Task.FromResult<FamilyGroupSummary?>(new FamilyGroupSummary
        {
            Id = "mock-group",
            Name = "Lottery Detection Demo",
            CreatedAt = DateTime.Today.AddDays(-30)
        });
    }

    public Task<IReadOnlyList<RoleOption>> GetRolesAsync()
    {
        IReadOnlyList<RoleOption> roles =
        [
            new() { Id = "owner", Label = "Owner", Description = "Can manage mock data", IsCurrent = true },
            new() { Id = "member", Label = "Member", Description = "Can scan and review tickets" }
        ];
        return Task.FromResult(roles);
    }

    public Task<FamilyMember?> InviteMemberAsync(string emailAddress, string role)
    {
        var member = new FamilyMember
        {
            Id = Guid.NewGuid().ToString(),
            Name = emailAddress.Split('@')[0],
            Email = emailAddress,
            Role = role,
            IsPending = true
        };
        members.Add(member);
        return Task.FromResult<FamilyMember?>(member);
    }

    public Task<FamilyMember?> UpdateMemberRoleAsync(string memberId, string role)
    {
        var member = members.FirstOrDefault(m => m.Id == memberId);
        if (member != null) member.Role = role;
        return Task.FromResult<FamilyMember?>(member);
    }

    public Task<bool> RemoveMemberAsync(string memberId)
    {
        members.RemoveAll(m => m.Id == memberId);
        return Task.FromResult(true);
    }

    public Task<bool> ResendInviteAsync(string memberId) => Task.FromResult(true);

    public Task<FamilyMember?> AcceptInvitationAsync(string token)
    {
        return Task.FromResult<FamilyMember?>(members.FirstOrDefault(m => m.IsYou) ?? members.FirstOrDefault());
    }
}
