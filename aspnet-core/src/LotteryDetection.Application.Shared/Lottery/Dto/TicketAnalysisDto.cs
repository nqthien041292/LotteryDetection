using System;
using Abp.Application.Services.Dto;

namespace LotteryDetection.Lottery.Dto;

public class TicketAnalysisDto : EntityDto<Guid>
{
    public Guid? ImageBinaryObjectId { get; set; }

    public string Province { get; set; }

    public DateTime? DrawDate { get; set; }

    public string TicketNumber { get; set; }

    public string DrawType { get; set; }

    public decimal? Confidence { get; set; }

    public bool? IsWinner { get; set; }

    public string MatchedPrize { get; set; }

    public decimal? PrizeAmount { get; set; }

    public string Notes { get; set; }

    public TicketAnalysisStatus Status { get; set; }

    public string ErrorMessage { get; set; }

    public DateTime CreationTime { get; set; }
}
