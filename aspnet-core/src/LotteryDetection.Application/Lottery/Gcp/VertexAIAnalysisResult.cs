using System;

namespace LotteryDetection.Lottery.Gcp;

public class VertexAIAnalysisResult
{
    public string Province { get; set; }
    public DateTime? DrawDate { get; set; }
    public string TicketNumber { get; set; }
    public string DrawType { get; set; }
    public decimal? Confidence { get; set; }
    public string Notes { get; set; }
    public string RawJson { get; set; }
}
