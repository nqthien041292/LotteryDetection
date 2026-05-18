using System.Collections.ObjectModel;
using System.Windows.Input;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.Services.Auth;

namespace LotteryDetectionMobile.ViewModel;

public class GamificationViewModel : TabNavigationViewModel
{
    private static readonly int[] LevelThresholds = { 0, 100, 220, 360, 520, 700, 920, 1180, 1480, 1820 };

    private readonly IGamificationService _gamificationService;
    private readonly IRewardService _rewardService;
    private readonly IAuthService? _authService;
    private readonly IFamilyMemberCache? _memberCache;

    private int xp;
    private int weekly;
    private int availableXp;
    private int streak;
    private bool hasLoaded;
    private bool isLoading;

    public GamificationViewModel()
        : this(NavigationService.Default, MockGamificationService.Instance, MockRewardService.Instance, null)
    {
    }

    public GamificationViewModel(
        INavigationService navigationService,
        IGamificationService gamificationService,
        IRewardService rewardService,
        IAuthService? authService = null,
        IFamilyMemberCache? memberCache = null)
        : base(navigationService)
    {
        _gamificationService = gamificationService;
        _rewardService = rewardService;
        _authService = authService;
        _memberCache = memberCache;
        Badges = new ObservableCollection<BadgeRow>();
        Leaderboard = new ObservableCollection<AchievementRow>();
        Rewards = new ObservableCollection<Reward>();

        BackCommand = new Command(async () => await navigationService.NavigateBackAsync());
        RedeemCommand = new Command<string>(async id => await RedeemAsync(id));
    }

    public ObservableCollection<BadgeRow> Badges { get; }
    public ObservableCollection<AchievementRow> Leaderboard { get; }
    public ObservableCollection<Reward> Rewards { get; }

    public ICommand BackCommand { get; }
    public ICommand RedeemCommand { get; }

    public bool IsLoading
    {
        get => isLoading;
        private set
        {
            if (SetProperty(ref isLoading, value))
                NotifyPropertyChanged(nameof(ShowSkeleton));
        }
    }

    public bool ShowSkeleton => IsLoading && !hasLoaded;

    private string myMember = string.Empty;
    public string MyMember
    {
        get => myMember;
        set => SetProperty(ref myMember, value);
    }

    private string myName = "User";
    public string MyName
    {
        get => myName;
        set
        {
            if (SetProperty(ref myName, value))
                NotifyPropertyChanged(nameof(MyInitial));
        }
    }

    public string MyInitial => string.IsNullOrEmpty(MyName) ? "U" : MyName[..1].ToUpperInvariant();

    public int Xp
    {
        get => xp;
        set
        {
            if (SetProperty(ref xp, value))
            {
                NotifyPropertyChanged(nameof(Level));
                NotifyPropertyChanged(nameof(LevelLabel));
                NotifyPropertyChanged(nameof(XpInLevel));
                NotifyPropertyChanged(nameof(XpToNextLevel));
                NotifyPropertyChanged(nameof(LevelProgressFraction));
                NotifyPropertyChanged(nameof(NextLevelLabel));
                NotifyPropertyChanged(nameof(XpDisplay));
            }
        }
    }

    public int AvailableXp
    {
        get => availableXp;
        set
        {
            if (SetProperty(ref availableXp, value))
                RefreshRewardsAffordability();
        }
    }

    public int Streak
    {
        get => streak;
        set => SetProperty(ref streak, value);
    }

    public int Weekly
    {
        get => weekly;
        set => SetProperty(ref weekly, value);
    }

    public int Level
    {
        get
        {
            var lvl = 1;
            for (var i = 0; i < LevelThresholds.Length; i++)
                if (xp >= LevelThresholds[i])
                    lvl = i + 1;
            return Math.Min(lvl, LevelThresholds.Length);
        }
    }

    public string LevelLabel => $"Level {Level}";
    public string NextLevelLabel => $"Level {Level + 1}";

    public int XpInLevel
    {
        get
        {
            var floor = LevelThresholds[Level - 1];
            return xp - floor;
        }
    }

    public int XpToNextLevel
    {
        get
        {
            var ceil = Level < LevelThresholds.Length ? LevelThresholds[Level] : LevelThresholds[^1] + 200;
            return Math.Max(0, ceil - xp);
        }
    }

    public double LevelProgressFraction
    {
        get
        {
            var floor = LevelThresholds[Level - 1];
            var ceil = Level < LevelThresholds.Length ? LevelThresholds[Level] : LevelThresholds[^1] + 200;
            if (ceil <= floor) return 0;
            return Math.Clamp((double)(xp - floor) / (ceil - floor), 0, 1);
        }
    }

    public string XpDisplay => $"{xp:N0} total XP";

    public string BadgesEarnedSummary => $"{Badges.Count(b => b.Earned)} of {Badges.Count}";

    public Task OnTabSelectedAsync(string? tabKey) => HandleTabSelectionAsync(tabKey);

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await InitializeInternalAsync();
            hasLoaded = true;
            NotifyPropertyChanged(nameof(ShowSkeleton));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task InitializeInternalAsync()
    {
        var rawName = _authService?.UserDisplayName;
        if (!string.IsNullOrWhiteSpace(rawName))
        {
            var first = rawName.Split(new[] { '.', ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                MyName = char.ToUpperInvariant(first[0]) + first[1..];
        }

        var pts = await _gamificationService.GetCurrentPointsAsync();
        if (pts > 0) Xp = pts;

        var weeklyPts = await _gamificationService.GetWeeklyPointsAsync();
        Weekly = weeklyPts;

        var availPts = await _gamificationService.GetAvailablePointsAsync();
        AvailableXp = availPts > 0 ? availPts : pts;

        var stk = await _gamificationService.GetCurrentStreakAsync();
        Streak = stk.Days > 0 ? stk.Days : Streak;

        Badges.Clear();
        var serverBadges = (await _gamificationService.GetBadgesAsync()).ToList();
        foreach (var b in MapServerBadges(serverBadges))
            Badges.Add(b);
        NotifyPropertyChanged(nameof(BadgesEarnedSummary));

        string? myMemberId = null;
        if (_memberCache != null)
        {
            try
            {
                var members = await _memberCache.GetMembersAsync();
                var userEmail = _authService?.UserEmail ?? string.Empty;
                var myMember = members.FirstOrDefault(m =>
                    string.Equals(m.Email, userEmail, StringComparison.OrdinalIgnoreCase));
                myMemberId = myMember?.Id;
                if (!string.IsNullOrEmpty(myMemberId))
                    MyMember = myMemberId;
                if (!string.IsNullOrWhiteSpace(myMember?.Name))
                {
                    MyName = myMember.Name;
                    Preferences.Set("user_display_name_override", myMember.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Gamification] MemberCache lookup error: {ex.Message}");
            }
        }

        Leaderboard.Clear();
        var leaderboardMembers = (await _gamificationService.GetLeaderboardAsync()).ToList();
        if (leaderboardMembers.Count > 0)
        {
            var ordered = leaderboardMembers.OrderByDescending(m => m.WeeklyPoints > 0 ? m.WeeklyPoints : m.Points).ToList();
            var medals = new[] { "🥇", "🥈", "🥉" };
            for (var i = 0; i < ordered.Count; i++)
                Leaderboard.Add(new AchievementRow
                {
                    MemberId = ordered[i].Id ?? ordered[i].Name.ToLowerInvariant(),
                    Name = ordered[i].Name,
                    Streak = ordered[i].Streak,
                    Weekly = ordered[i].WeeklyPoints > 0 ? ordered[i].WeeklyPoints : ordered[i].Points,
                    Rank = i + 1,
                    Medal = i < 3 ? medals[i] : $"#{i + 1}",
                    IsMe = myMemberId != null
                        ? string.Equals(ordered[i].Id, myMemberId, StringComparison.OrdinalIgnoreCase)
                        : (!string.IsNullOrWhiteSpace(ordered[i].Name) &&
                           string.Equals(ordered[i].Name, MyName, StringComparison.OrdinalIgnoreCase))
                });
        }

        Rewards.Clear();
        var serverRewards = (await _rewardService.GetRewardsAsync(AvailableXp)).ToList();
        foreach (var r in serverRewards)
            Rewards.Add(r);
    }

    private async Task RedeemAsync(string rewardId)
    {
        if (string.IsNullOrWhiteSpace(rewardId)) return;
        var result = await _rewardService.RedeemRewardAsync(rewardId);
        if (result.Success)
        {
            AvailableXp = result.NewAvailableXp;
        }
    }

    private static readonly Dictionary<string, Color> BadgeColors = new()
    {
        ["first"] = Color.FromArgb("#3B82F6"),
        ["week"] = Color.FromArgb("#EF6D2C"),
        ["helper"] = Color.FromArgb("#14B8A6"),
        ["early"] = Color.FromArgb("#F59E0B"),
        ["mvp"] = Color.FromArgb("#1E5BFF"),
        ["sweep"] = Color.FromArgb("#6D5BD0")
    };

    private static IEnumerable<BadgeRow> MapServerBadges(IEnumerable<Badge> badges)
    {
        foreach (var b in badges)
        {
            BadgeColors.TryGetValue(b.Id, out var color);
            yield return new BadgeRow
            {
                Id = b.Id,
                Emoji = b.Icon,
                Label = b.Title,
                Description = b.Description,
                Earned = b.IsUnlocked,
                BadgeColor = color ?? Color.FromArgb("#3B82F6")
            };
        }
    }

    private void RefreshRewardsAffordability()
    {
        for (var i = 0; i < Rewards.Count; i++)
        {
            var r = Rewards[i];
            r.Affordable = availableXp >= r.Cost;
            Rewards[i] = r;
        }
    }

}
