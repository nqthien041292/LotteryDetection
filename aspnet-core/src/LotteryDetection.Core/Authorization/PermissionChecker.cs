using Abp.Authorization;
using LotteryDetection.Authorization.Roles;
using LotteryDetection.Authorization.Users;

namespace LotteryDetection.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}