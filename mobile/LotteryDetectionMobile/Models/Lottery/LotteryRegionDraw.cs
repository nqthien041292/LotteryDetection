using System.Collections.ObjectModel;

namespace LotteryDetectionMobile.Models.Lottery;

public enum LotteryRegion
{
    Bac,
    Trung,
    Nam
}

public class LotteryRegionDraw
{
    public LotteryRegion Region { get; set; }
    public string RegionLabel { get; set; } = string.Empty;
    public string ProvinceLabel { get; set; } = string.Empty;
    public DateTime DrawDate { get; set; }
    public ObservableCollection<LotteryPrizeTier> Prizes { get; set; } = new();

    public string DrawDateDisplay => DrawDate.ToString("dd/MM/yyyy");

    // Strong accent (used for stripe, badge fill, special-prize box).
    public string AccentColor => Region switch
    {
        LotteryRegion.Bac => "#173D2A",
        LotteryRegion.Trung => "#B0521C",
        LotteryRegion.Nam => "#1E3A8A",
        _ => "#173D2A"
    };

    public string AccentTextColor => "White";

    // Soft tint for the card background.
    public string CardTintColor => Region switch
    {
        LotteryRegion.Bac => "#F1F8EB",
        LotteryRegion.Trung => "#FDF4E5",
        LotteryRegion.Nam => "#EEF2FF",
        _ => "#F1F8EB"
    };

    // Border stroke that matches the region accent at low opacity.
    public string CardStrokeColor => Region switch
    {
        LotteryRegion.Bac => "#C7E0BD",
        LotteryRegion.Trung => "#E9CDA1",
        LotteryRegion.Nam => "#C9D2F2",
        _ => "#C7E0BD"
    };

    // Small region icon hint (compass-like).
    public string RegionIcon => Region switch
    {
        LotteryRegion.Bac => "↑",
        LotteryRegion.Trung => "•",
        LotteryRegion.Nam => "↓",
        _ => "•"
    };

    public LotteryPrizeTier? SpecialPrize => Prizes.FirstOrDefault(p => p.IsSpecial);
    public IEnumerable<LotteryPrizeTier> OtherPrizes => Prizes.Where(p => !p.IsSpecial);
}

public class LotteryPrizeTier
{
    public string TierLabel { get; set; } = string.Empty;
    public string Numbers { get; set; } = string.Empty;
    public bool IsSpecial { get; set; }
}
