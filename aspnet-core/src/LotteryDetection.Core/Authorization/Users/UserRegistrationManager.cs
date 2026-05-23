using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.IdentityFramework;
using Abp.Linq;
using Abp.Notifications;
using Abp.Runtime.Session;
using Abp.UI;
using LotteryDetection.Authorization.Roles;
using LotteryDetection.Configuration;
using LotteryDetection.MultiTenancy;
using LotteryDetection.Notifications;
using Microsoft.AspNetCore.Identity;

namespace LotteryDetection.Authorization.Users;

public class UserRegistrationManager : LotteryDetectionDomainServiceBase
{
    private readonly IAppNotifier _appNotifier;
    private readonly INotificationSubscriptionManager _notificationSubscriptionManager;
    private readonly RoleManager _roleManager;

    private readonly TenantManager _tenantManager;
    private readonly IUserEmailer _userEmailer;
    private readonly UserManager _userManager;
    private readonly IUserPolicy _userPolicy;


    public UserRegistrationManager(
        TenantManager tenantManager,
        UserManager userManager,
        RoleManager roleManager,
        IUserEmailer userEmailer,
        INotificationSubscriptionManager notificationSubscriptionManager,
        IAppNotifier appNotifier,
        IUserPolicy userPolicy)
    {
        _tenantManager = tenantManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _userEmailer = userEmailer;
        _notificationSubscriptionManager = notificationSubscriptionManager;
        _appNotifier = appNotifier;
        _userPolicy = userPolicy;

        AbpSession = NullAbpSession.Instance;
        AsyncQueryableExecuter = NullAsyncQueryableExecuter.Instance;
    }

    public IAbpSession AbpSession { get; set; }
    public IAsyncQueryableExecuter AsyncQueryableExecuter { get; set; }

    public async Task<User> RegisterAsync(string name, string surname, string emailAddress, string userName,
        string plainPassword, bool isEmailConfirmed, string emailActivationLink)
    {
        CheckForTenant();
        CheckSelfRegistrationIsEnabled();
        await CheckForEmailDomainAsync(emailAddress);

        var tenant = await GetActiveTenantAsync();
        var tenantId = tenant?.Id;
        var isNewRegisteredUserActiveByDefault =
            await SettingManager.GetSettingValueAsync<bool>(AppSettings.UserManagement
                .IsNewRegisteredUserActiveByDefault);

        if (tenantId.HasValue)
        {
            await _userPolicy.CheckMaxUserCountAsync(tenantId.Value);
        }

        var user = new User
        {
            TenantId = tenantId,
            Name = name,
            Surname = surname,
            EmailAddress = emailAddress,
            IsActive = isNewRegisteredUserActiveByDefault,
            UserName = userName,
            IsEmailConfirmed = isEmailConfirmed,
            Roles = new List<UserRole>()
        };

        user.SetNormalizedNames();

        var defaultRoles = await AsyncQueryableExecuter.ToListAsync(_roleManager.Roles.Where(r => r.IsDefault && r.TenantId == tenantId));
        foreach (var defaultRole in defaultRoles) user.Roles.Add(new UserRole(tenantId, user.Id, defaultRole.Id));

        await _userManager.InitializeOptionsAsync(AbpSession.TenantId);
        CheckErrors(await _userManager.CreateAsync(user, plainPassword));
        await CurrentUnitOfWork.SaveChangesAsync();

        if (!user.IsEmailConfirmed)
        {
            user.SetNewEmailConfirmationCode();
            await _userEmailer.SendEmailActivationLinkAsync(user, emailActivationLink);
        }

        //Notifications
        await _notificationSubscriptionManager.SubscribeToAllAvailableNotificationsAsync(user.ToUserIdentifier());
        await _appNotifier.WelcomeToTheApplicationAsync(user);
        await _appNotifier.NewUserRegisteredAsync(user);

        return user;
    }

    private void CheckForTenant()
    {
        // Allowed to register host users
    }

    private void CheckSelfRegistrationIsEnabled()
    {
        if (!SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.AllowSelfRegistration))
            throw new UserFriendlyException(L("SelfUserRegistrationIsDisabledMessage_Detail"));
    }

    private bool UseCaptchaOnRegistration()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnRegistration);
    }

    private async Task<Tenant> GetActiveTenantAsync()
    {
        if (!AbpSession.TenantId.HasValue) return null;

        return await GetActiveTenantAsync(AbpSession.TenantId.Value);
    }

    private async Task<Tenant> GetActiveTenantAsync(int tenantId)
    {
        var tenant = await _tenantManager.FindByIdAsync(tenantId);
        if (tenant == null) throw new UserFriendlyException(L("UnknownTenantId{0}", tenantId));

        if (!tenant.IsActive) throw new UserFriendlyException(L("TenantIdIsNotActive{0}", tenantId));

        return tenant;
    }

    protected virtual void CheckErrors(IdentityResult identityResult)
    {
        identityResult.CheckErrors(LocalizationManager);
    }

    private async Task CheckForEmailDomainAsync(string emailAddress)
    {
        if (await IsRestrictedEmailDomainAllEnabledAsync())
        {
            var restrictedEmailDomain =
                await SettingManager.GetSettingValueAsync(AppSettings.UserManagement.RestrictedEmailDomain);
            var emailDomain = emailAddress.Split('@')[1];

            if (!emailDomain.Equals(restrictedEmailDomain, StringComparison.OrdinalIgnoreCase) &&
                restrictedEmailDomain != "")
                throw new UserFriendlyException(L("RestrictedEmailDomainInvalidMessage_Detail", emailAddress));
        }
    }

    private async Task<bool> IsRestrictedEmailDomainAllEnabledAsync()
    {
        var isRestrictedEmailDomainEnabledForApplication =
            await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AppSettings.TenantManagement.IsRestrictedEmailDomainEnabled);

        var isRestrictedEmailDomainEnabledForTenant = await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.IsRestrictedEmailDomainEnabled);

        return isRestrictedEmailDomainEnabledForApplication && isRestrictedEmailDomainEnabledForTenant;
    }
}