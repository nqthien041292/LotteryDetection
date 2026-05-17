using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.IdentityFramework;
using Abp.Runtime.Session;
using Abp.Threading;
using LotteryDetection.Authorization.Users;
using LotteryDetection.MultiTenancy;
using Microsoft.AspNetCore.Identity;

namespace LotteryDetection;

/// <summary>
///     Derive your application services from this class.
/// </summary>
public abstract class LotteryDetectionAppServiceBase : ApplicationService
{
    protected LotteryDetectionAppServiceBase()
    {
        LocalizationSourceName = LotteryDetectionConsts.LocalizationSourceName;
    }

    public TenantManager TenantManager { get; set; }

    public UserManager UserManager { get; set; }

    protected virtual async Task<User> GetCurrentUserAsync()
    {
        var user = await UserManager.FindByIdAsync(AbpSession.GetUserId().ToString());
        if (user == null) throw new Exception("There is no current user!");

        return user;
    }

    protected virtual User GetCurrentUser()
    {
        return AsyncHelper.RunSync(GetCurrentUserAsync);
    }

    protected virtual Task<Tenant> GetCurrentTenantAsync()
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            return TenantManager.GetByIdAsync(AbpSession.GetTenantId());
        }
    }

    protected virtual Tenant GetCurrentTenant()
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            return TenantManager.GetById(AbpSession.GetTenantId());
        }
    }

    protected virtual void CheckErrors(IdentityResult identityResult)
    {
        identityResult.CheckErrors(LocalizationManager);
    }
}