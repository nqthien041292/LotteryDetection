using System;
using System.Threading;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using LotteryDetection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LotteryDetection.HealthChecks;

public class LotteryDetectionDbContextUsersHealthCheck : IHealthCheck
{
    private readonly IDbContextProvider<LotteryDetectionDbContext> _dbContextProvider;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LotteryDetectionDbContextUsersHealthCheck(
        IDbContextProvider<LotteryDetectionDbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager
    )
    {
        _dbContextProvider = dbContextProvider;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        try
        {
            using (var uow = _unitOfWorkManager.Begin())
            {
                // Switching to host is necessary for single tenant mode.
                using (_unitOfWorkManager.Current.SetTenantId(null))
                {
                    var dbContext = await _dbContextProvider.GetDbContextAsync();
                    if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                        return HealthCheckResult.Unhealthy(
                            "LotteryDetectionDbContext could not connect to database"
                        );

                    var user = await dbContext.Users.AnyAsync(cancellationToken);
                    await uow.CompleteAsync();

                    if (user)
                        return HealthCheckResult.Healthy(
                            "LotteryDetectionDbContext connected to database and checked whether user added");

                    return HealthCheckResult.Unhealthy(
                        "LotteryDetectionDbContext connected to database but there is no user.");
                }
            }
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("LotteryDetectionDbContext could not connect to database.", e);
        }
    }
}