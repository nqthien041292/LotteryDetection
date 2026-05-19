using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IFamilyAuditLogService
{
    Task<IEnumerable<AuditEntry>> GetAuditLogAsync();
}
