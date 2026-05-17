namespace LotteryDetection.Configuration.Dto;

public class ThemeSubHeaderSettingsDto
{
    public ThemeSubHeaderSettingsDto()
    {
        SubheaderSize = 2;
    }

    public bool FixedSubHeader { get; set; }

    public string SubheaderStyle { get; set; }

    /// <summary>
    ///     A value between 1-6
    /// </summary>
    public int SubheaderSize { get; set; }

    public string TitleStyle { get; set; }

    public string ContainerStyle { get; set; }

    public string SubContainerStyle { get; set; }
}