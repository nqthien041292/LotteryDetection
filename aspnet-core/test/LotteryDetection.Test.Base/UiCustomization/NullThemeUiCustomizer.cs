using System;
using System.Threading.Tasks;
using Abp;
using LotteryDetection.Configuration.Dto;
using LotteryDetection.UiCustomization;
using LotteryDetection.UiCustomization.Dto;

namespace LotteryDetection.Test.Base.UiCustomization;

public class NullThemeUiCustomizer : IUiCustomizer
{
    public Task<UiCustomizationSettingsDto> GetUiSettings()
    {
        return Task.FromResult(new UiCustomizationSettingsDto());
    }

    public Task UpdateUserUiManagementSettingsAsync(UserIdentifier user, ThemeSettingsDto settings)
    {
        throw new NotImplementedException();
    }

    public Task UpdateTenantUiManagementSettingsAsync(int tenantId, ThemeSettingsDto settings,
        UserIdentifier changerUser)
    {
        throw new NotImplementedException();
    }

    public Task UpdateApplicationUiManagementSettingsAsync(ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        throw new NotImplementedException();
    }

    public Task<ThemeSettingsDto> GetHostUiManagementSettings()
    {
        throw new NotImplementedException();
    }

    public Task<ThemeSettingsDto> GetTenantUiCustomizationSettings(int tenantId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateDarkModeSettingsAsync(UserIdentifier user, bool isDarkModeEnabled)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetBodyClass()
    {
        throw new NotImplementedException();
    }

    public Task<string> GetBodyStyle()
    {
        throw new NotImplementedException();
    }
}