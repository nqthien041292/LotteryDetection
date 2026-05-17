using Abp.MultiTenancy;
using LotteryDetection.Url;

namespace LotteryDetection.Web.Url;

public class AngularAppUrlService : AppUrlServiceBase
{
    public AngularAppUrlService(
        IWebUrlService webUrlService,
        ITenantCache tenantCache
    ) : base(
        webUrlService,
        tenantCache
    )
    {
    }

    public override string EmailActivationRoute => "account/confirm-email";

    public override string EmailChangeRequestRoute => "account/change-email";

    public override string PasswordResetRoute => "account/reset-password";
}