using Abp.Configuration;

namespace LotteryDetection.Timing.Dto;

public class GetTimezoneComboboxItemsInput
{
    public SettingScopes DefaultTimezoneScope;

    public string SelectedTimezoneId { get; set; }
}

