using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.OpenIddict.EntityFrameworkCore.Authorizations;
using LotteryDetection.EntityFrameworkCore;

namespace LotteryDetection.OpenIddict.Authorizations;

public class OpenIddictAuthorizationRepository : EfCoreOpenIddictAuthorizationRepository<LotteryDetectionDbContext>
{
    public OpenIddictAuthorizationRepository(
        IDbContextProvider<LotteryDetectionDbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager) : base(dbContextProvider, unitOfWorkManager)
    {
    }
}

