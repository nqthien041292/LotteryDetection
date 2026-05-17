using Abp.Auditing;
using LotteryDetection.Configuration.Dto;

namespace LotteryDetection.Configuration.Tenants.Dto;

public class TenantEmailSettingsEditDto : EmailSettingsEditDto
{
    public bool UseHostDefaultEmailSettings { get; set; }
}

