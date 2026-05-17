using System.Threading;
using System.Threading.Tasks;
using LotteryDetection.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LotteryDetection.HealthChecks;

public class LotteryDetectionDbContextHealthCheck : IHealthCheck
{
    private readonly DatabaseCheckHelper _checkHelper;

    public LotteryDetectionDbContextHealthCheck(DatabaseCheckHelper checkHelper)
    {
        _checkHelper = checkHelper;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        if (_checkHelper.Exist("db"))
            return Task.FromResult(HealthCheckResult.Healthy("LotteryDetectionDbContext connected to database."));

        return Task.FromResult(HealthCheckResult.Unhealthy("LotteryDetectionDbContext could not connect to database"));
    }
}