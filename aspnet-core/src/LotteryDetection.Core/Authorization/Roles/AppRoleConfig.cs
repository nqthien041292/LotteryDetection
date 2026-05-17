using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace LotteryDetection.Authorization.Roles;

public static class AppRoleConfig
{
    public static void Configure(IRoleManagementConfig roleManagementConfig)
    {
        //Static host roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Admin,
                MultiTenancySides.Host,
                true)
        );

        //Static tenant roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Admin,
                MultiTenancySides.Tenant,
                true)
        );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.User,
                MultiTenancySides.Tenant)
        );
    }
}