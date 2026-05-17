using Abp.Dependency;

namespace Abp.AspNetZeroCore;

public class AspNetZeroConfiguration : ITransientDependency
{
    public string LicenseCode { get; set; }
}