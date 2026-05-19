using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace LotteryDetection.Lottery;

[Table("AppTicketAnalyses")]
public class TicketAnalysis : FullAuditedEntity<Guid>, IMayHaveTenant
{
    public const int ProvinceMaxLength = 128;
    public const int TicketNumberMaxLength = 256;
    public const int DrawTypeMaxLength = 64;
    public const int MatchedPrizeMaxLength = 128;
    public const int NotesMaxLength = 1024;
    public const int ErrorMessageMaxLength = 1024;
    public const int RawModelResponseMaxLength = 8000;

    public virtual int? TenantId { get; set; }

    public virtual Guid? ImageBinaryObjectId { get; set; }

    [MaxLength(ProvinceMaxLength)]
    public virtual string Province { get; set; }

    public virtual DateTime? DrawDate { get; set; }

    [MaxLength(TicketNumberMaxLength)]
    public virtual string TicketNumber { get; set; }

    [MaxLength(DrawTypeMaxLength)]
    public virtual string DrawType { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public virtual decimal? Confidence { get; set; }

    public virtual bool? IsWinner { get; set; }

    [MaxLength(MatchedPrizeMaxLength)]
    public virtual string MatchedPrize { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal? PrizeAmount { get; set; }

    [MaxLength(NotesMaxLength)]
    public virtual string Notes { get; set; }

    public virtual TicketAnalysisStatus Status { get; set; }

    [MaxLength(ErrorMessageMaxLength)]
    public virtual string ErrorMessage { get; set; }

    [MaxLength(RawModelResponseMaxLength)]
    public virtual string RawModelResponse { get; set; }
}
