using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Loading placeholder for CalendarPage: a faux month-grid card plus a faux selected-day
///     events card. A soft highlight crest travels left→right, recoloring each placeholder
///     block as it passes (no moving overlay, no layout inflation). Mirrors the design tokens
///     used by CalendarMonthGrid (paper card, cream-3 border, cream-2 blocks).
/// </summary>
public sealed class CalendarSkeleton : ContentView
{
    // Crest half-width in normalized [0,1] space; how many blocks light up at once.
    private const double Band = 0.34;

    // One full left→right pass. The crest overshoots both edges so it enters and
    // exits smoothly instead of popping in at x=0. Tuned so a complete pass is
    // visible within the skeleton's minimum display window.
    private const int SweepMs = 1400;

    private static readonly string[] DowLabels = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    private readonly Border gridCard;
    private readonly Border dayCard;

    // Each placeholder + its normalized horizontal centre (0 = left edge, 1 = right edge).
    private readonly List<(BoxView block, double pos)> blocks = new();
    private readonly List<Label> textLabels = new();

    private Color fill = Color.FromArgb("#E5E7EB");
    private Color highlight = Color.FromArgb("#F3F4F6");
    private int sweepEpoch;

    public CalendarSkeleton()
    {
        gridCard = BuildCard(BuildGrid());
        dayCard = BuildCard(BuildDayCard());

        Content = new VerticalStackLayout
        {
            Spacing = 14,
            Children = { gridCard, dayCard }
        };

        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();

        // OnPropertyChanged only fires on an IsVisible *transition*; if the control
        // attaches already-visible, kick the loop here.
        Loaded += (_, _) => { if (IsVisible) StartShimmer(); };
        Unloaded += (_, _) => StopShimmer();
    }

    private static Border BuildCard(View content) => new()
    {
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Padding = 0,
        Content = content
    };

    private View BuildGrid()
    {
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(8, 12, 8, 12),
            Spacing = 8
        };

        var dow = new Grid { ColumnSpacing = 0, Margin = new Thickness(0, 0, 0, 4) };
        for (var i = 0; i < 7; i++) dow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (var i = 0; i < 7; i++)
        {
            var lbl = new Label
            {
                Text = DowLabels[i],
                FontFamily = "Geist",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.06,
                HorizontalTextAlignment = TextAlignment.Center
            };
            textLabels.Add(lbl);
            Grid.SetColumn(lbl, i);
            dow.Children.Add(lbl);
        }
        stack.Children.Add(dow);

        var grid = new Grid { RowSpacing = 2, ColumnSpacing = 0 };
        for (var i = 0; i < 7; i++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (var r = 0; r < 6; r++) grid.RowDefinitions.Add(new RowDefinition(46));
        for (var r = 0; r < 6; r++)
        for (var c = 0; c < 7; c++)
        {
            // Cell centre across 7 star columns → smooth crest travel.
            var cell = Block(26, 26, 13, (c + 0.5) / 7.0);
            cell.HorizontalOptions = LayoutOptions.Center;
            cell.VerticalOptions = LayoutOptions.Center;
            Grid.SetRow(cell, r);
            Grid.SetColumn(cell, c);
            grid.Children.Add(cell);
        }
        stack.Children.Add(grid);
        return stack;
    }

    private View BuildDayCard()
    {
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(16, 16, 16, 18),
            Spacing = 14
        };
        var title = new Label
        {
            Text = DateTime.Today.ToString("dddd, MMM d"),
            FontFamily = "Geist",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = -0.015
        };
        textLabels.Add(title);
        stack.Children.Add(title);
        for (var i = 0; i < 3; i++)
        {
            var row = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
            // Approx horizontal centres of the three blocks relative to card width.
            row.Children.Add(Block(40, 12, 4, 0.10));
            row.Children.Add(Block(4, 26, 2, 0.20));
            row.Children.Add(Block(170, 12, 4, 0.55));
            stack.Children.Add(row);
        }
        return stack;
    }

    private BoxView Block(double w, double h, double r, double pos)
    {
        var b = new BoxView
        {
            WidthRequest = w,
            HeightRequest = h,
            CornerRadius = (float)r
        };
        blocks.Add((b, pos));
        return b;
    }

    private void ApplyTheme()
    {
        var paper = Color.FromArgb("#F8FAFC");
        var border = Color.FromArgb("#E5E7EB");
        var ink = Color.FromArgb("#6B7280");
        fill = Color.FromArgb("#E5E7EB");

        highlight = Color.FromArgb("#F3F4F6");

        gridCard.BackgroundColor = paper;
        dayCard.BackgroundColor = paper;
        gridCard.Stroke = new SolidColorBrush(border);
        dayCard.Stroke = new SolidColorBrush(border);
        foreach (var (b, _) in blocks) b.BackgroundColor = fill;
        foreach (var l in textLabels) l.TextColor = ink;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            if (IsVisible) StartShimmer();
            else StopShimmer();
        }
    }

    // Epoch-guarded dispatcher timer. Each (re)start claims a new epoch; the
    // timer callback stops itself (returns false) once its epoch is stale or the
    // control is hidden, so rapid IsVisible toggles can't stack concurrent
    // shimmers. StartTimer guarantees the callback runs on the UI thread (safe
    // for BackgroundColor writes) and has no dependency on native-handler /
    // animation-manager attach timing, so it runs reliably regardless of when
    // IsVisible flips relative to Loaded.
    private void StartShimmer()
    {
        var epoch = ++sweepEpoch;
        var sw = Stopwatch.StartNew();
        var dispatcher = Dispatcher ?? Application.Current?.Dispatcher;
        dispatcher?.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (epoch != sweepEpoch || !IsVisible)
                return false;
            var t = sw.ElapsedMilliseconds % SweepMs / (double)SweepMs;
            ApplyWave(-0.25 + t * 1.5);
            return true;
        });
    }

    private void StopShimmer()
    {
        sweepEpoch++;
        foreach (var (b, _) in blocks) b.BackgroundColor = fill;
    }

    // Recolour every block by its distance from the travelling crest. A smoothstep
    // falloff makes each block ease into the crest colour then settle back to the
    // resting fill as the crest passes.
    private void ApplyWave(double crest)
    {
        foreach (var (b, pos) in blocks)
        {
            var d = Math.Abs(pos - crest);
            var t = Math.Max(0.0, 1.0 - d / Band);
            t = t * t * (3.0 - 2.0 * t);
            b.BackgroundColor = Lerp(fill, highlight, t);
        }
    }

    private static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return Color.FromRgba(
            a.Red + (b.Red - a.Red) * t,
            a.Green + (b.Green - a.Green) * t,
            a.Blue + (b.Blue - a.Blue) * t,
            a.Alpha + (b.Alpha - a.Alpha) * t);
    }
}
