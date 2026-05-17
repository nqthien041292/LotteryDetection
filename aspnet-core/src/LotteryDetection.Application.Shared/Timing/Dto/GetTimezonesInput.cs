using Abp.Configuration;

namespace LotteryDetection.Timing.Dto;

public class GetTimezonesInput
{
    public SettingScopes DefaultTimezoneScope { get; set; }
}

