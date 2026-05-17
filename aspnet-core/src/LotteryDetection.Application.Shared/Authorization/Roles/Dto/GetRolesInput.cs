using System.Collections.Generic;

namespace LotteryDetection.Authorization.Roles.Dto;

public class GetRolesInput
{
    public List<string> Permissions { get; set; }
}