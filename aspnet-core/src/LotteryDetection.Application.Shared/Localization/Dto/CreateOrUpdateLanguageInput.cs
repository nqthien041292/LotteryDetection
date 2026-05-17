using System.ComponentModel.DataAnnotations;

namespace LotteryDetection.Localization.Dto;

public class CreateOrUpdateLanguageInput
{
    [Required]
    public ApplicationLanguageEditDto Language { get; set; }
}

