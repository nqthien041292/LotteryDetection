using System;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Runtime.Security;
using Abp.UI;
using LotteryDetection.Authorization;
using LotteryDetection.Authorization.Delegation;
using LotteryDetection.Authorization.Roles;
using LotteryDetection.Authorization.Users;
using LotteryDetection.MultiTenancy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LotteryDetection.Identity;

public class SecurityStampValidator : AbpSecurityStampValidator<Tenant, Role, User>
{
    private readonly PermissionChecker _permissionChecker;
    private readonly IUserDelegationConfiguration _userDelegationConfiguration;
    private readonly IUserDelegationManager _userDelegationManager;

    public SecurityStampValidator(
        IOptions<SecurityStampValidatorOptions> options,
        SignInManager signInManager,
        ILoggerFactory loggerFactory,
        IUserDelegationConfiguration userDelegationConfiguration,
        IUserDelegationManager userDelegationManager,
        PermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager)
        : base(options, signInManager, loggerFactory, unitOfWorkManager)
    {
        _userDelegationConfiguration = userDelegationConfiguration;
        _userDelegationManager = userDelegationManager;
        _permissionChecker = permissionChecker;
    }

    public override Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ValidateUserDelegation(context);

        return base.ValidateAsync(context);
    }

    private void ValidateUserDelegation(CookieValidatePrincipalContext context)
    {
        if (!_userDelegationConfiguration.IsEnabled) return;

        var impersonatorTenant =
            context.Principal.Claims.FirstOrDefault(c => c.Type == AbpClaimTypes.ImpersonatorTenantId);
        var user = context.Principal.Claims.FirstOrDefault(c => c.Type == AbpClaimTypes.UserId);
        var impersonatorUser = context.Principal.Claims.FirstOrDefault(c => c.Type == AbpClaimTypes.ImpersonatorUserId);

        if (impersonatorUser == null || user == null) return;

        var impersonatorTenantId = impersonatorTenant == null ? null :
            impersonatorTenant.Value.IsNullOrEmpty() ? (int?)null : Convert.ToInt32(impersonatorTenant.Value);
        var sourceUserId = Convert.ToInt64(user.Value);
        var targetUserId = Convert.ToInt64(impersonatorUser.Value);

        if (_permissionChecker.IsGranted(new UserIdentifier(impersonatorTenantId, targetUserId),
                AppPermissions.Pages_Administration_Users_Impersonation)) return;

        var hasActiveDelegation = _userDelegationManager.HasActiveDelegation(sourceUserId, targetUserId);

        if (!hasActiveDelegation)
            throw new UserFriendlyException("ThereIsNoActiveUserDelegationBetweenYourUserAndCurrentUser");
    }
}