using Abp.Modules;
using Abp.Reflection.Extensions;

namespace Abp.AspNetZeroCore;

public class AbpAspNetZeroCoreModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetZeroCoreModule).GetAssembly());
    }
}