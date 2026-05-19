using System.ComponentModel.DataAnnotations;

namespace LotteryDetection.Lottery.Dto;

public class AnalyzeTicketInput
{
    [Required]
    public byte[] ImageBytes { get; set; }

    [Required]
    [StringLength(64)]
    public string ContentType { get; set; }

    [StringLength(128)]
    public string FileName { get; set; }
}
