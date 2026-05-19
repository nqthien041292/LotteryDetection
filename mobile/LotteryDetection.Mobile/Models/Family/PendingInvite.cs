namespace LotteryDetection.Mobile.Models.Family;

public class PendingInvite
{
    public string MemberId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string InvitedAtRelative { get; set; } = string.Empty;

    public string Subtitle => $"Invited as {Role} · {InvitedAtRelative}";
}
