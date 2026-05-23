using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.EntityFrameworkCore;

public static class LotteryDetectionDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<LotteryDetectionDbContext> builder, string connectionString)
    {
        builder.UseNpgsql(connectionString);
        builder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    public static void Configure(DbContextOptionsBuilder<LotteryDetectionDbContext> builder, DbConnection connection)
    {
        builder.UseNpgsql(connection);
        builder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
}