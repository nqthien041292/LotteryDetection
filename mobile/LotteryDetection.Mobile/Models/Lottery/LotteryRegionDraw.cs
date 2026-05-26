using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LotteryDetection.Mobile.Models.Lottery;

public enum LotteryRegion
{
    Bac,
    Trung,
    Nam
}

public class LotteryProvinceHeader
{
    public string Name { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
}

public class LotteryNumberCol
{
    public string Number { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
}

public class LotteryRowDraw
{
    public string TierLabel { get; set; } = string.Empty;
    public bool IsSpecial { get; set; }
    public ObservableCollection<LotteryNumberCol> Numbers { get; set; } = new(); // Numbers for each province in this tier
}

public class LotteryRegionDraw
{
    public LotteryRegion Region { get; set; }
    public string RegionLabel { get; set; } = string.Empty;
    public string ProvinceLabel { get; set; } = string.Empty;
    public DateTime DrawDate { get; set; }

    public ObservableCollection<LotteryProvinceHeader> Provinces { get; set; } = new();
    public ObservableCollection<LotteryRowDraw> Rows { get; set; } = new();

    public bool IsVietlott { get; set; }
    public string VietlottType { get; set; } = string.Empty; // "Max3D", "Max3D+", "Max3DPro"
    public string DrawId { get; set; } = string.Empty; // e.g., "#01068"
    public string DayOfWeekLabel => DrawDate.ToString("dddd");

    public string ColumnDefinitions => string.Join(",", Provinces.Select(_ => "*"));

    // For backwards compatibility
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
    public bool IsDrawn { get; set; } = true;
    public bool IsNotDrawn => !IsDrawn;

    public List<string> NumberList => Numbers.Split(new[] { ' ', '·' }, StringSplitOptions.RemoveEmptyEntries).ToList();

    public List<List<string>> DigitGroups => Numbers.Split(new[] { ' ', '·' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(n => n.Select(c => c.ToString()).ToList())
        .ToList();
}
