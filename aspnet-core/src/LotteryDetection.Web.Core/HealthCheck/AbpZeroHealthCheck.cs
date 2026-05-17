using LotteryDetection.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Web.HealthCheck;

public static class AbpZeroHealthCheck
{
    public static IHealthChecksBuilder AddAbpZeroHealthCheck(this IServiceCollection services)
    {
        var builder = services.AddHealthChecks();
        builder.AddCheck<LotteryDetectionDbContextHealthCheck>("Database Connection");
        builder.AddCheck<LotteryDetectionDbContextUsersHealthCheck>("Database Connection with user check");
        builder.AddCheck<CacheHealthCheck>("Cache");

        // add your custom health checks here
        // builder.AddCheck<MyCustomHealthCheck>("my health check");

        return builder;
    }
}