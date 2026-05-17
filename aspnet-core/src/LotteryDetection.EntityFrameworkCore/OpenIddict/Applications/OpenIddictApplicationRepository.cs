using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.OpenIddict.EntityFrameworkCore.Applications;
using LotteryDetection.EntityFrameworkCore;

namespace LotteryDetection.OpenIddict.Applications;

public class OpenIddictApplicationRepository : EfCoreOpenIddictApplicationRepository<LotteryDetectionDbContext>
{
    public OpenIddictApplicationRepository(
        IDbContextProvider<LotteryDetectionDbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager) : base(dbContextProvider, unitOfWorkManager)
    {
    }
}

