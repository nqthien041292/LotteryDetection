using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.OpenIddict.EntityFrameworkCore.Scopes;
using LotteryDetection.EntityFrameworkCore;

namespace LotteryDetection.OpenIddict.Scopes;

public class OpenIddictScopeRepository : EfCoreOpenIddictScopeRepository<LotteryDetectionDbContext>
{
    public OpenIddictScopeRepository(
        IDbContextProvider<LotteryDetectionDbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager) : base(dbContextProvider, unitOfWorkManager)
    {
    }
}