using System.Collections.Generic;

namespace LotteryDetection.Configuration.Dto;

public class ExternalLoginSettingsDto
{
    public List<string> EnabledSocialLoginSettings { get; set; } = new();
}