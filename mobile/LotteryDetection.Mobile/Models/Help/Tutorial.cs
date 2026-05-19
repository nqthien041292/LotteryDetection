namespace LotteryDetection.Mobile.Models.Help;

public class Tutorial
{
    public string Title { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Accent { get; set; } = "primary";

    public string TintColor { get; set; } = "#E0EAFF";
    public string ForegroundColor { get; set; } = "#1E5BFF";
    public string IconGlyph { get; set; } = "▶";
}
