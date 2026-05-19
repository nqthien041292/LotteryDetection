namespace LotteryDetection.Mobile.Models.Family;

public class FamilyMember
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsPending { get; set; }
    public string Avatar { get; set; } = string.Empty;
    public int Points { get; set; }
    public int WeeklyPoints { get; set; }
    public int Streak { get; set; }

    public int? Age { get; set; }
    public bool IsYou { get; set; }

    public string Subtitle =>
        Age is int age && age > 0
            ? $"{Email} · age {age}"
            : Email;
}
