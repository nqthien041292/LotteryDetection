using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Models.Help;

public class TopicChip : BaseViewModel
{
    private bool isSelected;

    public string Label { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
