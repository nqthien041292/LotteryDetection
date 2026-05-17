using System.Collections.Generic;

namespace LotteryDetection.Authorization.Users.Profile.Dto;

public class UpdateGoogleAuthenticatorKeyOutput
{
    public IEnumerable<string> RecoveryCodes { get; set; }
}