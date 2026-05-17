using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LotteryDetection.Authorization.Users.Dto;

public class CreateOrUpdateUserInput
{
    public CreateOrUpdateUserInput()
    {
        OrganizationUnits = new List<long>();
    }

    [Required] public UserEditDto User { get; set; }

    [Required] public string[] AssignedRoleNames { get; set; }

    public bool SendActivationEmail { get; set; }

    public bool SetRandomPassword { get; set; }

    public List<long> OrganizationUnits { get; set; }
}