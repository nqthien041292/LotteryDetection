using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Models.Family;

public class FaqItem : BaseViewModel
{
    private bool isOpen;

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    public bool IsOpen
    {
        get => isOpen;
        set => SetProperty(ref isOpen, value);
    }
}
