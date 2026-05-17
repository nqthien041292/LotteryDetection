using System.Collections.Generic;
using LotteryDetection.Authorization.Permissions.Dto;

namespace LotteryDetection.Authorization.Users.Dto;

public class GetUserPermissionsForEditOutput
{
    public List<FlatPermissionDto> Permissions { get; set; }

    public List<string> GrantedPermissionNames { get; set; }
}