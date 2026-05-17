using System.Collections.Generic;
using LotteryDetection.Authorization.Permissions.Dto;

namespace LotteryDetection.Authorization.Roles.Dto;

public class GetRoleForEditOutput
{
    public RoleEditDto Role { get; set; }

    public List<FlatPermissionDto> Permissions { get; set; }

    public List<string> GrantedPermissionNames { get; set; }
}

