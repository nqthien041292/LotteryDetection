using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Mock;

public sealed class MockFamilyAuditLogService : IFamilyAuditLogService
{
    public static IFamilyAuditLogService Instance { get; } = new MockFamilyAuditLogService();

    public Task<IEnumerable<AuditEntry>> GetAuditLogAsync()
    {
        var entries = new[]
        {
            new AuditEntry
            {
                IconGlyph = "scan",
                What = "Scanned sample lottery ticket",
                Who = "Lottery Demo",
                TintColor = "#E7F6DE",
                ForegroundColor = "#173D2A"
            },
            new AuditEntry
            {
                IconGlyph = "check",
                What = "Compared ticket against mock result set",
                Who = "System",
                TintColor = "#FFF4C9",
                ForegroundColor = "#7A6412"
            }
        };
        return Task.FromResult(entries.AsEnumerable());
    }
}
