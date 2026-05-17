namespace LotteryDetection.Configuration.Dto;

public class ThemeSettingsDto
{
    public string Theme { get; set; }

    public ThemeLayoutSettingsDto Layout { get; set; } = new();

    public ThemeHeaderSettingsDto Header { get; set; } = new();

    public ThemeSubHeaderSettingsDto SubHeader { get; set; } = new();

    public ThemeMenuSettingsDto Menu { get; set; } = new();

    public ThemeFooterSettingsDto Footer { get; set; } = new();

    public ThemeToolbarSettingsDto Toolbar { get; set; } = new();
}