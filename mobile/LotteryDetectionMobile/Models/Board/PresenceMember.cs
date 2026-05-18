namespace LotteryDetectionMobile.Models.Board;

/// <summary>One avatar bubble in the presence cluster.</summary>
public sealed class PresenceMember
{
    public string Member { get; init; } = "home";
    public string Initial { get; init; } = "?";
    public bool IsOnline { get; init; }
}
