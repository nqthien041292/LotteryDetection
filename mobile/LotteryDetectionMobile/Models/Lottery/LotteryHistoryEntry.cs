namespace LotteryDetectionMobile.Models.Lottery;

public class LotteryHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CapturedAt { get; set; }
    public string Province { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public DateTime DrawDate { get; set; }
    public bool IsWinner { get; set; }
    public string? MatchedPrize { get; set; }
    public long? PrizeAmount { get; set; }

    public string CapturedDisplay => CapturedAt.Date == DateTime.Today
        ? $"Hôm nay · {CapturedAt:HH:mm}"
        : CapturedAt.Date == DateTime.Today.AddDays(-1)
            ? $"Hôm qua · {CapturedAt:HH:mm}"
            : CapturedAt.ToString("dd/MM · HH:mm");

    public string DrawDateDisplay => DrawDate.ToString("dd/MM/yyyy");
    public string PrizeAmountDisplay => PrizeAmount.HasValue ? $"{PrizeAmount.Value:N0} đ" : "—";

    public string StatusLabel => IsWinner ? (MatchedPrize ?? "Đã trúng") : "Chưa trúng";
    public string StatusBackground => IsWinner ? "#F5C842" : "#F2F4F0";
    public string StatusTextColor => IsWinner ? "#173D2A" : "#66736A";
    public string TicketBadgeBackground => IsWinner ? "#F5C842" : "#E9EFE4";
    public string TicketBadgeTextColor => IsWinner ? "#173D2A" : "#173D2A";

    // Winner-specific visual emphasis used in the redesigned history list.
    public string CardBackground => IsWinner ? "#173D2A" : "White";
    public string CardStrokeColor => IsWinner ? "#173D2A" : "#DAE5D6";
    public string PrimaryTextColor => IsWinner ? "White" : "#142116";
    public string SecondaryTextColor => IsWinner ? "#CFE6D5" : "#38443B";
    public string MutedTextColor => IsWinner ? "#A9D7B6" : "#8A958D";
    public string TicketNumberColor => IsWinner ? "#F5C842" : "#142116";
    public string PrizeAmountColor => IsWinner ? "#F5C842" : "#173D2A";
    public double PrizeAmountFontSize => IsWinner ? 18 : 12;
    public bool ShowTrophy => IsWinner;
    public string TicketBadgeIcon => IsWinner ? "🏆" : "VÉ";
    public double TicketBadgeFontSize => IsWinner ? 22 : 13;
}
