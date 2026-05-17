using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.EntityFrameworkCore;

public static class LotteryDetectionDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<LotteryDetectionDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<LotteryDetectionDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}

