using Abp.AspNetCore.Mvc.Authorization;
using LotteryDetection.Authorization.Users.Profile;
using LotteryDetection.Storage;

namespace LotteryDetection.Web.Controllers;

[AbpMvcAuthorize]
public class ProfileController : ProfileControllerBase
{
    public ProfileController(
        ITempFileCacheManager tempFileCacheManager,
        IProfileAppService profileAppService) :
        base(tempFileCacheManager, profileAppService)
    {
    }
}

