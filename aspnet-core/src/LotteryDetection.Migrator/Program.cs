using System;
using Abp;
using Abp.Castle.Logging.Log4Net;
using Abp.Collections.Extensions;
using Abp.Dependency;
using Castle.Facilities.Logging;

namespace LotteryDetection.Migrator;

public class Program
{
    private static bool _skipConnVerification;

    public static void Main(string[] args)
    {
        ParseArgs(args);

        bool.TryParse(Environment.GetEnvironmentVariable("ASPNETCORE_Docker_Enabled"), out var isDockerEnabled);

        using (var bootstrapper = AbpBootstrapper.Create<LotteryDetectionMigratorModule>())
        {
            bootstrapper.IocManager.IocContainer
                .AddFacility<LoggingFacility>(f => f.UseAbpLog4Net()
                    .WithConfig("log4net.config")
                );

            bootstrapper.Initialize();

            using (var migrateExecuter = bootstrapper.IocManager.ResolveAsDisposable<MultiTenantMigrateExecuter>())
            {
                migrateExecuter.Object.Run(_skipConnVerification, isDockerEnabled);
            }

            if (_skipConnVerification || isDockerEnabled) return;

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }
    }

    private static void ParseArgs(string[] args)
    {
        if (args.IsNullOrEmpty()) return;

        foreach (var arg in args)
            if (arg == "-s")
                _skipConnVerification = true;
    }
}