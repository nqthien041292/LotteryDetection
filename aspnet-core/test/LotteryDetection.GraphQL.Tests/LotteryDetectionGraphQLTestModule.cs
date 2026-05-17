using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.Windsor.MsDependencyInjection;
using LotteryDetection.Configure;
using LotteryDetection.Startup;
using LotteryDetection.Test.Base;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.GraphQL.Tests;

[DependsOn(
    typeof(LotteryDetectionGraphQLModule),
    typeof(LotteryDetectionTestBaseModule))]
public class LotteryDetectionGraphQLTestModule : AbpModule
{
    public override void PreInitialize()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddAndConfigureGraphQL();

        WindsorRegistrationHelper.CreateServiceProvider(IocManager.IocContainer, services);
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LotteryDetectionGraphQLTestModule).GetAssembly());
    }
}