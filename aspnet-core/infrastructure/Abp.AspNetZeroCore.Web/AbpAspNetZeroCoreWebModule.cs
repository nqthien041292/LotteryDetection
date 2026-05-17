using Abp.AspNetCore;
using Abp.Modules;

namespace Abp.AspNetZeroCore.Web;

[DependsOn(typeof(AbpAspNetZeroCoreModule))]
[DependsOn(typeof(AbpAspNetCoreModule))]
public class AbpAspNetZeroCoreWebModule : AbpModule
{
    public override void PreInitialize()
    {
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetZeroCoreWebModule).Assembly);
    }
}