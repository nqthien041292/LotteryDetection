using LotteryDetectionMobile.Models.Family;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IFamilyAuditLogService
{
    Task<IEnumerable<AuditEntry>> GetAuditLogAsync();
}
