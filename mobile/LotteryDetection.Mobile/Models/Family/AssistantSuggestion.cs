namespace LotteryDetection.Mobile.Models.Family;

public class AssistantSuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // create | conflict | delegate
    public string Kind { get; set; } = "create";

    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    // alex | sam | jordan | riley | home
    public string Member { get; set; } = "home";
    public string MemberName { get; set; } = "Shared";

    // low | med | high
    public string Priority { get; set; } = "med";
    public bool IsHigh => string.Equals(Priority, "high", StringComparison.OrdinalIgnoreCase);

    public bool IsConflict { get; set; }
    public string Elapsed { get; set; } = "0.4s";
}
