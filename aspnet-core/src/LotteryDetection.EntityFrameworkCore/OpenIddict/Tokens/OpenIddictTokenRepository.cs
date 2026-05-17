using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.OpenIddict.EntityFrameworkCore.Tokens;
using LotteryDetection.EntityFrameworkCore;

namespace LotteryDetection.OpenIddict.Tokens;

public class OpenIddictTokenRepository : EfCoreOpenIddictTokenRepository<LotteryDetectionDbContext>
{
    public OpenIddictTokenRepository(
        IDbContextProvider<LotteryDetectionDbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager) : base(dbContextProvider, unitOfWorkManager)
    {
    }
}

