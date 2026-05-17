using System.Threading.Tasks;
using Abp;
using Abp.Configuration;
using Abp.Localization;
using LotteryDetection.Configuration.Dto;
using LotteryDetection.UiCustomization;
using LotteryDetection.UiCustomization.Dto;
using static LotteryDetection.Configuration.AppSettings.UiManagement;

namespace LotteryDetection.Web.UiCustomization.Metronic;

public class ThemeDefaultUiCustomizer : UiThemeCustomizerBase, IUiCustomizer
{
    private readonly ILocalizationManager _localizationManager;

    public ThemeDefaultUiCustomizer(
        ISettingManager settingManager,
        ILocalizationManager localizationManager)
        : base(settingManager, AppConsts.ThemeDefault)
    {
        _localizationManager = localizationManager;
    }

    public async Task<UiCustomizationSettingsDto> GetUiSettings()
    {
        var settings = new UiCustomizationSettingsDto
        {
            BaseSettings = new ThemeSettingsDto
            {
                Layout = new ThemeLayoutSettingsDto
                {
                    LayoutType = await GetSettingValueAsync(LayoutType),
                    DarkMode = await GetSettingValueAsync<bool>(DarkMode)
                },
                SubHeader = new ThemeSubHeaderSettingsDto
                {
                    FixedSubHeader = true,
                    SubheaderStyle = await GetSettingValueAsync(SubHeader.Style),
                    ContainerStyle = "app-toolbar py-3 py-lg-6"
                },
                Menu = new ThemeMenuSettingsDto
                {
                    AsideSkin = await GetSettingValueAsync(LeftAside.AsideSkin),
                    FixedAside = true,
                    AllowAsideMinimizing =
                        await GetSettingValueAsync<bool>(LeftAside.AllowAsideMinimizing),
                    DefaultMinimizedAside =
                        await GetSettingValueAsync<bool>(LeftAside.DefaultMinimizedAside),
                    SubmenuToggle = await GetSettingValueAsync(LeftAside.SubmenuToggle),
                    HoverableAside =
                        await GetSettingValueAsync<bool>(LeftAside.HoverableAside),
                    SearchActive = await GetSettingValueAsync<bool>(SearchActive)
                },
                Footer = new ThemeFooterSettingsDto
                {
                    DesktopFixedFooter = await GetSettingValueAsync<bool>(Footer.DesktopFixedFooter),
                    MobileFixedFooter = await GetSettingValueAsync<bool>(Footer.MobileFixedFooter)
                },
                Toolbar = new ThemeToolbarSettingsDto
                {
                    DesktopFixedToolbar = await GetSettingValueAsync<bool>(Toolbar.DesktopFixedToolbar),
                    MobileFixedToolbar = await GetSettingValueAsync<bool>(Toolbar.MobileFixedToolbar)
                }
            }
        };

        settings.BaseSettings.Theme = ThemeName;
        settings.BaseSettings.Footer.FooterWidthType = settings.BaseSettings.Layout.LayoutType;
        settings.BaseSettings.Menu.Position = "left";
        settings.BaseSettings.SubHeader.SubheaderSize = 1;
        settings.BaseSettings.SubHeader.TitleStyle =
            "page-heading d-flex text-gray-900 fw-bold fs-3 flex-column justify-content-center my-0";
        settings.BaseSettings.SubHeader.ContainerStyle = "app-toolbar py-3 py-lg-6";

        settings.IsLeftMenuUsed = true;
        settings.IsTopMenuUsed = false;
        settings.IsTabMenuUsed = false;

        return settings;
    }

    public async Task UpdateUserUiManagementSettingsAsync(UserIdentifier user, ThemeSettingsDto settings)
    {
        await SettingManager.ChangeSettingForUserAsync(user, Theme, ThemeName);

        await ChangeSettingForUserAsync(user, DarkMode,
            settings.Layout.DarkMode.ToString());
        await ChangeSettingForUserAsync(user, LayoutType,
            settings.Layout.LayoutType);

        await ChangeSettingForUserAsync(user, Header.DesktopFixedHeader,
            settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForUserAsync(user, Header.MobileFixedHeader,
            settings.Header.MobileFixedHeader.ToString());

        await ChangeSettingForUserAsync(user, SubHeader.Fixed,
            settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForUserAsync(user, SubHeader.Style,
            settings.SubHeader.SubheaderStyle);

        await ChangeSettingForUserAsync(user, LeftAside.AsideSkin,
            settings.Menu.AsideSkin);
        await ChangeSettingForUserAsync(user, LeftAside.AllowAsideMinimizing,
            settings.Menu.AllowAsideMinimizing.ToString());
        await ChangeSettingForUserAsync(user, LeftAside.DefaultMinimizedAside,
            settings.Menu.DefaultMinimizedAside.ToString());
        await ChangeSettingForUserAsync(user, LeftAside.SubmenuToggle,
            settings.Menu.SubmenuToggle);
        await ChangeSettingForUserAsync(user, LeftAside.HoverableAside,
            settings.Menu.HoverableAside.ToString());
        await ChangeSettingForUserAsync(user, SearchActive,
            settings.Menu.SearchActive.ToString());

        await ChangeSettingForUserAsync(user, Footer.DesktopFixedFooter,
            settings.Footer.DesktopFixedFooter.ToString());
        await ChangeSettingForUserAsync(user, Footer.MobileFixedFooter,
            settings.Footer.MobileFixedFooter.ToString());

        await ChangeSettingForUserAsync(user, Toolbar.DesktopFixedToolbar,
            settings.Toolbar.DesktopFixedToolbar.ToString()
        );
        await ChangeSettingForUserAsync(user, Toolbar.MobileFixedToolbar,
            settings.Toolbar.MobileFixedToolbar.ToString()
        );
    }

    public async Task UpdateTenantUiManagementSettingsAsync(int tenantId, ThemeSettingsDto settings,
        UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForTenantAsync(tenantId, Theme, settings.Theme);

        await ChangeSettingForTenantAsync(tenantId, DarkMode,
            settings.Layout.DarkMode.ToString());
        await ChangeSettingForTenantAsync(tenantId, LayoutType,
            settings.Layout.LayoutType);

        await ChangeSettingForTenantAsync(tenantId, Header.DesktopFixedHeader,
            settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, Header.MobileFixedHeader,
            settings.Header.MobileFixedHeader.ToString());

        await ChangeSettingForTenantAsync(tenantId, SubHeader.Fixed,
            settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, SubHeader.Style,
            settings.SubHeader.SubheaderStyle);

        await ChangeSettingForTenantAsync(tenantId, LeftAside.AsideSkin,
            settings.Menu.AsideSkin);
        await ChangeSettingForTenantAsync(tenantId, LeftAside.AllowAsideMinimizing,
            settings.Menu.AllowAsideMinimizing.ToString());
        await ChangeSettingForTenantAsync(tenantId, LeftAside.DefaultMinimizedAside,
            settings.Menu.DefaultMinimizedAside.ToString());
        await ChangeSettingForTenantAsync(tenantId, LeftAside.SubmenuToggle,
            settings.Menu.SubmenuToggle);
        await ChangeSettingForTenantAsync(tenantId, LeftAside.HoverableAside,
            settings.Menu.HoverableAside.ToString());
        await ChangeSettingForTenantAsync(tenantId, SearchActive,
            settings.Menu.SearchActive.ToString());

        await ChangeSettingForTenantAsync(tenantId, Footer.DesktopFixedFooter,
            settings.Footer.DesktopFixedFooter.ToString());
        await ChangeSettingForTenantAsync(tenantId, Footer.MobileFixedFooter,
            settings.Footer.MobileFixedFooter.ToString());

        await ChangeSettingForTenantAsync(tenantId, Toolbar.DesktopFixedToolbar,
            settings.Toolbar.DesktopFixedToolbar.ToString());
        await ChangeSettingForTenantAsync(tenantId, Toolbar.MobileFixedToolbar,
            settings.Toolbar.MobileFixedToolbar.ToString());

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task UpdateApplicationUiManagementSettingsAsync(ThemeSettingsDto settings,
        UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForApplicationAsync(Theme, settings.Theme);

        await ChangeSettingForApplicationAsync(DarkMode,
            settings.Layout.DarkMode.ToString());
        await ChangeSettingForApplicationAsync(LayoutType,
            settings.Layout.LayoutType);

        await ChangeSettingForApplicationAsync(Header.DesktopFixedHeader,
            settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForApplicationAsync(Header.MobileFixedHeader,
            settings.Header.MobileFixedHeader.ToString());

        await ChangeSettingForApplicationAsync(SubHeader.Fixed,
            settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForApplicationAsync(SubHeader.Style,
            settings.SubHeader.SubheaderStyle);

        await ChangeSettingForApplicationAsync(LeftAside.AsideSkin,
            settings.Menu.AsideSkin);
        await ChangeSettingForApplicationAsync(LeftAside.AllowAsideMinimizing,
            settings.Menu.AllowAsideMinimizing.ToString());
        await ChangeSettingForApplicationAsync(LeftAside.DefaultMinimizedAside,
            settings.Menu.DefaultMinimizedAside.ToString());
        await ChangeSettingForApplicationAsync(LeftAside.SubmenuToggle,
            settings.Menu.SubmenuToggle);
        await ChangeSettingForApplicationAsync(LeftAside.HoverableAside,
            settings.Menu.HoverableAside.ToString());
        await ChangeSettingForApplicationAsync(SearchActive,
            settings.Menu.SearchActive.ToString());

        await ChangeSettingForApplicationAsync(Footer.DesktopFixedFooter,
            settings.Footer.DesktopFixedFooter.ToString());
        await ChangeSettingForApplicationAsync(Footer.MobileFixedFooter,
            settings.Footer.MobileFixedFooter.ToString());

        await ChangeSettingForApplicationAsync(Toolbar.DesktopFixedToolbar,
            settings.Toolbar.DesktopFixedToolbar.ToString());
        await ChangeSettingForApplicationAsync(Toolbar.MobileFixedToolbar,
            settings.Toolbar.MobileFixedToolbar.ToString());

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task<ThemeSettingsDto> GetHostUiManagementSettings()
    {
        var theme = await SettingManager.GetSettingValueForApplicationAsync(Theme);

        return new ThemeSettingsDto
        {
            Theme = theme,
            Layout = new ThemeLayoutSettingsDto
            {
                LayoutType = await GetSettingValueForApplicationAsync(LayoutType),
                DarkMode = await GetSettingValueForApplicationAsync<bool>(DarkMode)
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader =
                    await GetSettingValueForApplicationAsync<bool>(SubHeader.Fixed),
                SubheaderStyle = await GetSettingValueForApplicationAsync(SubHeader.Style)
            },
            Menu = new ThemeMenuSettingsDto
            {
                AsideSkin = await GetSettingValueForApplicationAsync(LeftAside.AsideSkin),
                FixedAside = true,
                AllowAsideMinimizing =
                    await GetSettingValueForApplicationAsync<bool>(LeftAside
                        .AllowAsideMinimizing),
                DefaultMinimizedAside =
                    await GetSettingValueForApplicationAsync<bool>(LeftAside
                        .DefaultMinimizedAside),
                SubmenuToggle =
                    await GetSettingValueForApplicationAsync(LeftAside.SubmenuToggle),
                HoverableAside =
                    await GetSettingValueForApplicationAsync<bool>(
                        LeftAside.HoverableAside),
                SearchActive =
                    await GetSettingValueForApplicationAsync<bool>(SearchActive)
            },
            Footer = new ThemeFooterSettingsDto
            {
                DesktopFixedFooter =
                    await GetSettingValueForApplicationAsync<bool>(Footer.DesktopFixedFooter),

                MobileFixedFooter =
                    await GetSettingValueForApplicationAsync<bool>(Footer.MobileFixedFooter)
            },
            Toolbar = new ThemeToolbarSettingsDto
            {
                DesktopFixedToolbar = await GetSettingValueForApplicationAsync<bool>(Toolbar.DesktopFixedToolbar),
                MobileFixedToolbar = await GetSettingValueForApplicationAsync<bool>(Toolbar.MobileFixedToolbar)
            }
        };
    }

    public async Task<ThemeSettingsDto> GetTenantUiCustomizationSettings(int tenantId)
    {
        var theme = await SettingManager.GetSettingValueForTenantAsync(Theme, tenantId);

        return new ThemeSettingsDto
        {
            Theme = theme,
            Layout = new ThemeLayoutSettingsDto
            {
                LayoutType = await GetSettingValueForTenantAsync(LayoutType, tenantId),
                DarkMode = await GetSettingValueForTenantAsync<bool>(DarkMode, tenantId)
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader =
                    await GetSettingValueForTenantAsync<bool>(SubHeader.Fixed, tenantId),
                SubheaderStyle =
                    await GetSettingValueForTenantAsync(SubHeader.Style, tenantId)
            },
            Menu = new ThemeMenuSettingsDto
            {
                AsideSkin = await GetSettingValueForTenantAsync(LeftAside.AsideSkin,
                    tenantId),
                FixedAside = true,
                AllowAsideMinimizing =
                    await GetSettingValueForTenantAsync<bool>(
                        LeftAside.AllowAsideMinimizing, tenantId),
                DefaultMinimizedAside =
                    await GetSettingValueForTenantAsync<bool>(
                        LeftAside.DefaultMinimizedAside, tenantId),
                SubmenuToggle =
                    await GetSettingValueForTenantAsync(LeftAside.SubmenuToggle, tenantId),
                HoverableAside =
                    await GetSettingValueForTenantAsync<bool>(LeftAside.HoverableAside,
                        tenantId),
                SearchActive = await GetSettingValueForApplicationAsync<bool>(SearchActive)
            },
            Footer = new ThemeFooterSettingsDto
            {
                DesktopFixedFooter =
                    await GetSettingValueForTenantAsync<bool>(Footer.DesktopFixedFooter, tenantId),

                MobileFixedFooter =
                    await GetSettingValueForTenantAsync<bool>(Footer.MobileFixedFooter, tenantId)
            },
            Toolbar = new ThemeToolbarSettingsDto
            {
                DesktopFixedToolbar = await GetSettingValueForTenantAsync<bool>(Toolbar.DesktopFixedToolbar, tenantId),
                MobileFixedToolbar = await GetSettingValueForTenantAsync<bool>(Toolbar.MobileFixedToolbar, tenantId)
            }
        };
    }

    protected override async Task ResetDarkModeSettingsAsync(UserIdentifier user)
    {
        await base.ResetDarkModeSettingsAsync(user);

        string asideSkinSetting;
        if (user.TenantId.HasValue)
            asideSkinSetting = await GetSettingValueForTenantAsync(LeftAside.AsideSkin,
                user.TenantId.Value);
        else
            asideSkinSetting =
                await GetSettingValueForApplicationAsync(LeftAside.AsideSkin);

        await ChangeSettingForUserAsync(user, LeftAside.AsideSkin, asideSkinSetting);
    }
}