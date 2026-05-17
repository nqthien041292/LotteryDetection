using Abp.Dependency;
using Castle.MicroKernel.Registration;
using Castle.Windsor.MsDependencyInjection;
using LotteryDetection.EntityFrameworkCore;
using LotteryDetection.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Test.Base.DependencyInjection;

public static class ServiceCollectionRegistrar
{
    public static void Register(IIocManager iocManager)
    {
        RegisterIdentity(iocManager);

        var builder = new DbContextOptionsBuilder<LotteryDetectionDbContext>();

        var inMemorySqlite = new SqliteConnection("Data Source=:memory:");
        builder.UseSqlite(inMemorySqlite);

        iocManager.IocContainer.Register(
            Component
                .For<DbContextOptions<LotteryDetectionDbContext>>()
                .Instance(builder.Options)
                .LifestyleSingleton()
        );

        inMemorySqlite.Open();

        new LotteryDetectionDbContext(builder.Options).Database.EnsureCreated();
    }

    private static void RegisterIdentity(IIocManager iocManager)
    {
        var services = new ServiceCollection();

        IdentityRegistrar.Register(services);

        WindsorRegistrationHelper.CreateServiceProvider(iocManager.IocContainer, services);
    }
}