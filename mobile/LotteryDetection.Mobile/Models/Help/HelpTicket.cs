namespace LotteryDetection.Mobile.Models.Help;

public class HelpTicket
{
    public string Topic { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
