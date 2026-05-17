using System.Collections.Generic;
using Abp.Extensions;
using Abp.Localization;
using LotteryDetection.Sessions.Dto;

namespace LotteryDetection.Web.Public.Views.Shared.Components.Header;

public class HeaderViewModel
{
    public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

    public IReadOnlyList<LanguageInfo> Languages { get; set; }

    public LanguageInfo CurrentLanguage { get; set; }

    public string CurrentPageName { get; set; }

    public bool IsMultiTenancyEnabled { get; set; }

    public bool TenantRegistrationEnabled { get; set; }

    public bool IsInHostView { get; set; }

    public string AdminWebSiteRootAddress { get; set; }

    public string WebSiteRootAddress { get; set; }

    public string LogoSkin { get; set; }

    public string GetShownLoginName()
    {
        if (!IsMultiTenancyEnabled) return LoginInformations.User.UserName;

        return LoginInformations.Tenant == null
            ? ".\\" + LoginInformations.User.UserName
            : LoginInformations.Tenant.TenancyName + "\\" + LoginInformations.User.UserName;
    }

    public string GetLogoUrl(string appPath, string logoSkin)
    {
        if (!IsMultiTenancyEnabled || LoginInformations?.Tenant == null || !LoginInformations.Tenant.HasLogo())
            return appPath + "Common/Images/app-logo-on-" + logoSkin + ".svg";

        return AdminWebSiteRootAddress.EnsureEndsWith('/') + "TenantCustomization/GetTenantLogo?tenantId=" +
               LoginInformations?.Tenant?.Id + "&skin=" + logoSkin;
    }
}