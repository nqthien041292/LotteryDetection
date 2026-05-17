using System.ComponentModel.DataAnnotations;

namespace LotteryDetection.Authorization.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required] public string LanguageName { get; set; }
}