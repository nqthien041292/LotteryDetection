using LotteryDetection.Configuration;
using LotteryDetection.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.EntityFrameworkCore;

/* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
public class LotteryDetectionDbContextFactory : IDesignTimeDbContextFactory<LotteryDetectionDbContext>
{
    public LotteryDetectionDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LotteryDetectionDbContext>();

        /*
         You can provide an environmentName parameter to the AppConfigurations.Get method.
         In this case, AppConfigurations will try to read appsettings.{environmentName}.json.
         Use Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") method or from string[] args to get environment if necessary.
         https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#args
         */
        var configuration = AppConfigurations.Get(
            WebContentDirectoryFinder.CalculateContentRootFolder(),
            addUserSecrets: true
        );

        LotteryDetectionDbContextConfigurer.Configure(builder,
            configuration.GetConnectionString(LotteryDetectionConsts.ConnectionStringName));

        return new LotteryDetectionDbContext(builder.Options);
    }
}