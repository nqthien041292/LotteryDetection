using System.ComponentModel.DataAnnotations;

namespace LotteryDetection.Authorization.Users.Dto;

public class RegisterDeviceTokenInput
{
    [Required]
    [MaxLength(512)]
    public string Token { get; set; }

    [MaxLength(128)]
    public string DeviceType { get; set; }

    [MaxLength(128)]
    public string DeviceName { get; set; }
}
