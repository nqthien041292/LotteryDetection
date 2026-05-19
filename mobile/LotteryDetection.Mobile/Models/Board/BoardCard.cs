namespace LotteryDetection.Mobile.Models.Board;

/// <summary>
///     One Kanban card in the family board: title + assignee colour + when label + priority + optional live flag.
/// </summary>
public sealed class BoardCard
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = string.Empty;
    public string Member { get; init; } = "home";
    public string OwnerLabel { get; init; } = string.Empty;
    public string When { get; init; } = string.Empty;
    public string Priority { get; init; } = "med";
    public bool IsLive { get; init; }
    public string Status { get; set; } = "todo";

    public bool IsHigh => string.Equals(Priority, "high", StringComparison.OrdinalIgnoreCase);
    public bool IsDone => string.Equals(Status, "done", StringComparison.OrdinalIgnoreCase);
}
