namespace LotteryDetection.Mobile.Models.Lottery;

public class LotteryTicketResult
{
    public string Province { get; set; } = string.Empty;
    public DateTime DrawDate { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public bool IsWinner { get; set; }
    public string? MatchedPrize { get; set; }
    public long? PrizeAmount { get; set; }
    public double Confidence { get; set; }
    public string? Notes { get; set; }
    public string? ImagePath { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.Now;

    public string DrawDateDisplay => DrawDate.ToString("dd/MM/yyyy");
    public string ConfidenceDisplay => $"{Confidence * 100:0}%";
    public string PrizeAmountDisplay => PrizeAmount.HasValue ? $"{PrizeAmount.Value:N0} đ" : "—";
    public string ResultHeadline => IsWinner
        ? $"Trúng {MatchedPrize}"
        : "Chưa trúng giải";
}
