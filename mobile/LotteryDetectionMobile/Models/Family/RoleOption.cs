using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Models.Family;

public class RoleOption : BaseViewModel
{
    private bool isCurrent;

    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TintColor { get; set; } = "#E5E7EF";
    public string ForegroundColor { get; set; } = "#334155";

    public bool IsCurrent
    {
        get => isCurrent;
        set => SetProperty(ref isCurrent, value);
    }

    public string Initial => string.IsNullOrEmpty(Label) ? string.Empty : Label[..1].ToUpperInvariant();
}
