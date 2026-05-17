using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LotteryDetection.EntityFrameworkCore;

namespace LotteryDetection.HealthChecks;

public class LotteryDetectionDbContextHealthCheck : IHealthCheck
{
    private readonly DatabaseCheckHelper _checkHelper;

    public LotteryDetectionDbContextHealthCheck(DatabaseCheckHelper checkHelper)
    {
        _checkHelper = checkHelper;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_checkHelper.Exist("db"))
        {
            return Task.FromResult(HealthCheckResult.Healthy("LotteryDetectionDbContext connected to database."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("LotteryDetectionDbContext could not connect to database"));
    }
}
