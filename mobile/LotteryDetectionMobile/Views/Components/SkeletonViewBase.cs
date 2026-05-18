using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

public abstract class SkeletonViewBase : ContentView
{
    private const double Band = 0.34;
    private const int SweepMs = 1400;

    private readonly List<(BoxView block, double pos)> blocks = new();
    private Color fill = Color.FromArgb("#E5E7EB");
    private Color highlight = Color.FromArgb("#F3F4F6");
    private int sweepEpoch;

    protected SkeletonViewBase()
    {
        Loaded += (_, _) => { if (IsVisible) StartShimmer(); };
        Unloaded += (_, _) => StopShimmer();
        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => ApplyTheme();
    }

    protected void SetSkeletonContent(View content)
    {
        Content = content;
        ApplyTheme();
        if (IsVisible) StartShimmer();
    }

    protected void ResetSkeletonBlocks() => blocks.Clear();

    protected Border Card(View content, double height = -1, double radius = 16, Thickness? padding = null)
    {
        var card = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Padding = padding ?? new Thickness(14, 12),
            Content = content
        };
        if (height > 0)
            card.HeightRequest = height;
        return card;
    }

    protected BoxView Block(double width, double height, double radius = 4, double pos = 0.5)
    {
        var block = new BoxView
        {
            WidthRequest = width,
            HeightRequest = height,
            CornerRadius = (float)radius,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center
        };
        blocks.Add((block, pos));
        return block;
    }

    protected Grid Grid(params ColumnDefinition[] columns)
    {
        var grid = new Grid();
        foreach (var column in columns)
            grid.ColumnDefinitions.Add(column);
        return grid;
    }

    protected Grid RowGrid(params RowDefinition[] rows)
    {
        var grid = new Grid();
        foreach (var row in rows)
            grid.RowDefinitions.Add(row);
        return grid;
    }

    protected static ColumnDefinition StarColumn() => new(GridLength.Star);
    protected static ColumnDefinition AutoColumn() => new(GridLength.Auto);
    protected static ColumnDefinition FixedColumn(double width) => new(width);
    protected static RowDefinition AutoRow() => new(GridLength.Auto);
    protected static RowDefinition StarRow() => new(GridLength.Star);
    protected static RowDefinition FixedRow(double height) => new(height);

    protected View TextPair(double titleWidth, double subtitleWidth, double pos = 0.45)
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Block(titleWidth, 14, 4, pos),
                Block(subtitleWidth, 10, 4, pos - 0.06)
            }
        };
    }

    protected View ChipRow(params double[] widths)
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        for (var i = 0; i < widths.Length; i++)
            row.Children.Add(Block(widths[i], 30, 15, 0.12 + i * 0.18));
        return row;
    }

    protected View AvatarRow(double height = 74, bool trailing = true)
    {
        var grid = Grid(FixedColumn(42), StarColumn(), trailing ? FixedColumn(52) : AutoColumn());
        grid.ColumnSpacing = 12;
        grid.Add(Block(42, 42, 12, 0.08), 0);
        grid.Add(TextPair(176, 128, 0.44), 1);
        if (trailing)
            grid.Add(Block(34, 18, 8, 0.88), 2);
        return Card(grid, height);
    }

    protected View TaskRow(double height = 82)
    {
        var grid = Grid(StarColumn(), FixedColumn(42));
        grid.ColumnSpacing = 12;
        var text = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Block(210, 14, 4, 0.38),
                Block(164, 10, 4, 0.34),
                Block(118, 10, 4, 0.28)
            }
        };
        grid.Add(text, 0);
        grid.Add(Block(36, 22, 9, 0.88), 1);
        return Card(grid, height);
    }

    private void ApplyTheme()
    {
        var paper = Color.FromArgb("#F8FAFC");
        var border = Color.FromArgb("#E5E7EB");
        fill = Color.FromArgb("#E5E7EB");
        highlight = Color.FromArgb("#F3F4F6");

        if (Content != null)
            ApplyCardTheme(Content, paper, border);
        foreach (var (block, _) in blocks)
            block.BackgroundColor = fill;
    }

    private static void ApplyCardTheme(IView view, Color paper, Color border)
    {
        if (view is Border card)
        {
            card.BackgroundColor = paper;
            card.Stroke = new SolidColorBrush(border);
        }

        if (view is Layout layout)
        {
            foreach (var child in layout.Children)
                ApplyCardTheme(child, paper, border);
        }
        else if (view is ContentView contentView && contentView.Content != null)
        {
            ApplyCardTheme(contentView.Content, paper, border);
        }
        else if (view is Border borderView && borderView.Content != null)
        {
            ApplyCardTheme(borderView.Content, paper, border);
        }
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
        foreach (var (block, _) in blocks)
            block.BackgroundColor = fill;
    }

    private void ApplyWave(double crest)
    {
        foreach (var (block, pos) in blocks)
        {
            var d = Math.Abs(pos - crest);
            var t = Math.Max(0.0, 1.0 - d / Band);
            t = t * t * (3.0 - 2.0 * t);
            block.BackgroundColor = Lerp(fill, highlight, t);
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
