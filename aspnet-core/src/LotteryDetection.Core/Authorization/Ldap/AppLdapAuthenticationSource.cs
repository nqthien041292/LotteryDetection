using Abp.Zero.Ldap.Authentication;
using Abp.Zero.Ldap.Configuration;
using LotteryDetection.Authorization.Users;
using LotteryDetection.MultiTenancy;

namespace LotteryDetection.Authorization.Ldap;

public class AppLdapAuthenticationSource : LdapAuthenticationSource<Tenant, User>
{
    public AppLdapAuthenticationSource(ILdapSettings settings, IAbpZeroLdapModuleConfig ldapModuleConfig)
        : base(settings, ldapModuleConfig)
    {
    }
}

