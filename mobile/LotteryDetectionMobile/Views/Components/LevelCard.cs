using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Gradient blue level + streak summary card (per design "LevelCard"). Shown on the Today screen.
/// </summary>
public sealed class LevelCard : ContentView
{
    private static readonly int[] LevelThresholds = { 0, 100, 220, 360, 520, 700, 920, 1180, 1480, 1820 };

    public static readonly BindableProperty XpProperty =
        BindableProperty.Create(nameof(Xp), typeof(int), typeof(LevelCard), 0,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty StreakProperty =
        BindableProperty.Create(nameof(Streak), typeof(int), typeof(LevelCard), 0,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty WeeklyRankProperty =
        BindableProperty.Create(nameof(WeeklyRank), typeof(int), typeof(LevelCard), 0,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty WeeklyProperty =
        BindableProperty.Create(nameof(Weekly), typeof(int), typeof(LevelCard), 0,
            propertyChanged: OnVisualChanged);

    private readonly Label levelBadge;
    private readonly Label levelLabel;
    private readonly Label progressFooter;
    private readonly Border progressFill;
    private readonly Label rankLabel;
    private readonly Label remainingLabel;
    private readonly Label streakLabel;
    private readonly Label xpLabel;

    public LevelCard()
    {
        levelBadge = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        var levelTile = new Border
        {
            WidthRequest = 32,
            HeightRequest = 32,
            BackgroundColor = Color.FromArgb("#38FFFFFF"),
            Stroke = Color.FromArgb("#4DFFFFFF"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = levelBadge,
            VerticalOptions = LayoutOptions.Center
        };

        levelLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 10.5,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#CCFFFFFF"),
            CharacterSpacing = 0.06,
            TextTransform = TextTransform.Uppercase
        };
        xpLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            CharacterSpacing = -0.01
        };
        var levelTextStack = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children = { levelLabel, xpLabel }
        };

        var levelHeader = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children = { levelTile, levelTextStack }
        };

        streakLabel = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End
        };
        rankLabel = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End
        };
        var streakBlock = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                streakLabel,
                new Label
                {
                    Text = "DAY STREAK",
                    FontFamily = "Geist",
                    FontSize = 9.5,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#BFFFFFFF"),
                    CharacterSpacing = 0.04,
                    HorizontalOptions = LayoutOptions.End
                }
            }
        };
        var rankBlock = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                rankLabel,
                new Label
                {
                    Text = "THIS WEEK",
                    FontFamily = "Geist",
                    FontSize = 9.5,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#BFFFFFFF"),
                    CharacterSpacing = 0.04,
                    HorizontalOptions = LayoutOptions.End
                }
            }
        };

        var statsRow = new HorizontalStackLayout
        {
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            Children = { streakBlock, rankBlock }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        Grid.SetColumn(levelHeader, 0);
        Grid.SetColumn(statsRow, 1);
        headerGrid.Children.Add(levelHeader);
        headerGrid.Children.Add(statsRow);

        // Progress track
        progressFill = new Border
        {
            HeightRequest = 6,
            BackgroundColor = Color.FromArgb("#FFFFFFFF"),
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 99 }
        };
        var progressTrack = new Border
        {
            HeightRequest = 6,
            BackgroundColor = Color.FromArgb("#33FFFFFF"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 99 },
            Content = new Grid
            {
                Children = { progressFill }
            }
        };

        remainingLabel = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 10.5,
            TextColor = Color.FromArgb("#BFFFFFFF")
        };
        progressFooter = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 10.5,
            TextColor = Color.FromArgb("#BFFFFFFF"),
            HorizontalOptions = LayoutOptions.End
        };
        var progressFooterGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Margin = new Thickness(0, 6, 0, 0)
        };
        Grid.SetColumn(remainingLabel, 0);
        Grid.SetColumn(progressFooter, 1);
        progressFooterGrid.Children.Add(remainingLabel);
        progressFooterGrid.Children.Add(progressFooter);

        var content = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                headerGrid,
                progressTrack,
                progressFooterGrid
            }
        };

        var card = new Border
        {
            Padding = new Thickness(14),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Background = new LinearGradientBrush(new GradientStopCollection
            {
                new() { Color = Color.FromArgb("#1E5BFF"), Offset = 0f },
                new() { Color = Color.FromArgb("#2D6CFF"), Offset = 0.5f },
                new() { Color = Color.FromArgb("#6D5BD0"), Offset = 1f }
            }, new Point(0, 0), new Point(1, 1)),
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#1E5BFF")), Offset = new Point(0, 8), Radius = 20, Opacity = 0.28f },
            Content = content
        };

        Content = card;
        SizeChanged += (_, _) => UpdateProgressWidth();
        Apply();
    }

    public int Xp
    {
        get => (int)GetValue(XpProperty);
        set => SetValue(XpProperty, value);
    }

    public int Streak
    {
        get => (int)GetValue(StreakProperty);
        set => SetValue(StreakProperty, value);
    }

    public int WeeklyRank
    {
        get => (int)GetValue(WeeklyRankProperty);
        set => SetValue(WeeklyRankProperty, value);
    }

    public int Weekly
    {
        get => (int)GetValue(WeeklyProperty);
        set => SetValue(WeeklyProperty, value);
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LevelCard c)
        {
            c.Apply();
            c.UpdateProgressWidth();
        }
    }

    private void Apply()
    {
        var (lvl, cur, next) = ProgressFor(Xp);
        levelBadge.Text = lvl.ToString();
        levelLabel.Text = $"Level {lvl}";
        xpLabel.Text = $"{Xp:N0} XP";
        streakLabel.Text = $"🔥 {Streak}";
        rankLabel.Text = WeeklyRank > 0 ? $"🏅 #{WeeklyRank}" : "🏅 –";
        remainingLabel.Text = $"{Math.Max(0, next - Xp)} XP to Level {lvl + 1}";
        progressFooter.Text = $"{Xp - cur} / {next - cur}";
    }

    private void UpdateProgressWidth()
    {
        var (_, cur, next) = ProgressFor(Xp);
        var pct = next > cur ? Math.Clamp((Xp - cur) / (double)(next - cur), 0.04, 1.0) : 0.04;
        if (progressFill.Parent is Grid g && g.Width > 0) progressFill.WidthRequest = g.Width * pct;
    }

    private static (int level, int curThreshold, int nextThreshold) ProgressFor(int xp)
    {
        var lvl = 1;
        for (var i = 0; i < LevelThresholds.Length; i++)
            if (xp >= LevelThresholds[i])
                lvl = i + 1;
        lvl = Math.Min(lvl, LevelThresholds.Length);
        var cur = LevelThresholds[lvl - 1];
        var next = lvl < LevelThresholds.Length ? LevelThresholds[lvl] : cur + 200;
        return (lvl, cur, next);
    }
}
