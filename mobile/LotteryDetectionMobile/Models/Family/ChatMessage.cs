namespace LotteryDetectionMobile.Models.Family;

public class ChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsUser { get; set; }
}