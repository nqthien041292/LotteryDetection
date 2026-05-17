using System.Linq;
using Abp.MultiTenancy;
using LotteryDetection.Editions;
using LotteryDetection.EntityFrameworkCore;
using LotteryDetection.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.Migrations.Seed.Tenants;

public class DefaultTenantBuilder
{
    private readonly LotteryDetectionDbContext _context;

    public DefaultTenantBuilder(LotteryDetectionDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateDefaultTenant();
    }

    private void CreateDefaultTenant()
    {
        //Default tenant

        var defaultTenant = _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefault(t => t.TenancyName == Tenant.DefaultTenantName);
        if (defaultTenant == null)
        {
            defaultTenant = new Tenant(AbpTenantBase.DefaultTenantName, AbpTenantBase.DefaultTenantName);

            var defaultEdition = _context.Editions.IgnoreQueryFilters()
                .FirstOrDefault(e => e.Name == EditionManager.DefaultEditionName);
            if (defaultEdition != null) defaultTenant.EditionId = defaultEdition.Id;

            _context.Tenants.Add(defaultTenant);
            _context.SaveChanges();
        }
    }
}