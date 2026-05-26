using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace LotteryDetection.Authorization.Users;

[Table("AppUserDeviceTokens")]
public class UserDeviceToken : CreationAuditedEntity<long>
{
    public long UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    [Required]
    [MaxLength(512)]
    public string Token { get; set; }

    [MaxLength(128)]
    public string DeviceType { get; set; } // ios, android

    [MaxLength(128)]
    public string DeviceName { get; set; }
}
