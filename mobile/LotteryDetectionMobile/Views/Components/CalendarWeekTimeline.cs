using System.Collections;
using System.Collections.Specialized;
using LotteryDetectionMobile.Models.Calendar;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Bundle screens-calendar.jsx → WeekTimeline. Day-of-week header row + 7 absolute-layout
///     day columns 8a-9p with member-tinted event blocks and a now-line on today.
/// </summary>
public sealed class CalendarWeekTimeline : ContentView
{
    private const int StartHour = 8;
    private const int EndHour = 21;
    private const double HourPx = 30;

    public static readonly BindableProperty DaysProperty =
        BindableProperty.Create(nameof(Days), typeof(IEnumerable), typeof(CalendarWeekTimeline),
            propertyChanged: OnSourceChanged);

    public static readonly BindableProperty BlocksProperty =
        BindableProperty.Create(nameof(Blocks), typeof(IEnumerable), typeof(CalendarWeekTimeline),
            propertyChanged: OnSourceChanged);

    private readonly Border card;
    private readonly Grid headerRow;
    private readonly Grid timelineRow;

    public CalendarWeekTimeline()
    {
        headerRow = BuildSevenColRow(marginBottom: 6);
        timelineRow = BuildSevenColRow(marginBottom: 0);
        timelineRow.HeightRequest = (EndHour - StartHour) * HourPx;

        var stack = new VerticalStackLayout
        {
            Spacing = 0,
            Padding = new Thickness(10, 10, 10, 10),
            Children = { headerRow, timelineRow }
        };

        card = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = 0,
            Content = stack
        };
        Content = card;

        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public IEnumerable? Days
    {
        get => (IEnumerable?)GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    public IEnumerable? Blocks
    {
        get => (IEnumerable?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    private static Grid BuildSevenColRow(int marginBottom)
    {
        var g = new Grid
        {
            ColumnSpacing = 2,
            Margin = new Thickness(0, 0, 0, marginBottom)
        };
        g.ColumnDefinitions.Add(new ColumnDefinition(32));
        for (var i = 0; i < 7; i++) g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        return g;
    }

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not CalendarWeekTimeline w) return;
        if (oldValue is INotifyCollectionChanged oldNcc) oldNcc.CollectionChanged -= w.OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newNcc) newNcc.CollectionChanged += w.OnCollectionChanged;
        w.Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void ApplyTheme()
    {
        var paper = ResourceLookup.Color("FamilyPaperLight", "FamilyPaperDark", Colors.White);
        var border = ResourceLookup.Color("FamilyCream3Light", "FamilyCream3Dark", Color.FromArgb("#D5E1F2"));
        card.BackgroundColor = paper;
        card.Stroke = new SolidColorBrush(border);
        Rebuild();
    }

    private void Rebuild()
    {
        headerRow.Children.Clear();
        timelineRow.Children.Clear();

        var ink = ResourceLookup.Color("FamilyInkLight", "FamilyInkDark", Color.FromArgb("#0A1628"));
        var ink4 = ResourceLookup.Color("FamilyInk4Light", "FamilyInk4Dark", Color.FromArgb("#94A3BE"));
        var cream2 = ResourceLookup.Color("FamilyCream2Light", "FamilyCream2Dark", Color.FromArgb("#F1EDDC"));
        var todayCol = Color.FromArgb(Application.Current?.RequestedTheme == AppTheme.Dark ? "#1B2537" : "#FAFCFF");
        var hourLine = Color.FromArgb("#2E94A3BE");
        var accent = ResourceLookup.Color("FamilyPrimaryLight", "FamilyPrimaryDark", Color.FromArgb("#1E5BFF"));

        // ── Header row ──
        var corner = new BoxView { BackgroundColor = Colors.Transparent };
        Grid.SetColumn(corner, 0);
        headerRow.Children.Add(corner);

        var days = Days?.Cast<WeekDayHeader>().ToList() ?? new List<WeekDayHeader>();
        for (var i = 0; i < 7; i++)
        {
            var day = i < days.Count ? days[i] : null;
            var stack = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };
            stack.Children.Add(new Label
            {
                Text = day?.Label.ToUpperInvariant() ?? string.Empty,
                FontFamily = "Geist",
                FontSize = 9,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.06,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = ink4
            });
            stack.Children.Add(new Border
            {
                WidthRequest = 22,
                HeightRequest = 22,
                StrokeThickness = 0,
                BackgroundColor = day?.IsToday == true ? ink : Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 11 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = day?.DayNumber.ToString() ?? string.Empty,
                    FontFamily = "Geist",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = day?.IsToday == true ? Colors.White : ink
                }
            });
            Grid.SetColumn(stack, i + 1);
            headerRow.Children.Add(stack);
        }

        // ── Hour-label column ──
        var hourCol = new AbsoluteLayout();
        for (var h = StartHour; h <= EndHour; h += 2)
        {
            var lbl = new Label
            {
                Text = h > 12 ? $"{h - 12}p" : $"{h}a",
                FontFamily = "GeistMono",
                FontSize = 9,
                FontAttributes = FontAttributes.Bold,
                TextColor = ink4
            };
            AbsoluteLayout.SetLayoutBounds(lbl,
                new Rect(0, (h - StartHour) * HourPx - 5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(lbl, AbsoluteLayoutFlags.None);
            hourCol.Children.Add(lbl);
        }
        Grid.SetColumn(hourCol, 0);
        timelineRow.Children.Add(hourCol);

        // ── 7 day columns ──
        var blocks = Blocks?.Cast<WeekEventBlock>().ToList() ?? new List<WeekEventBlock>();
        for (var i = 0; i < 7; i++)
        {
            var day = i < days.Count ? days[i] : null;
            var col = new AbsoluteLayout
            {
                BackgroundColor = day?.IsToday == true ? todayCol : cream2
            };

            for (var h = StartHour; h <= EndHour; h += 2)
            {
                var line = new BoxView { BackgroundColor = hourLine, HeightRequest = 1 };
                AbsoluteLayout.SetLayoutBounds(line,
                    new Rect(0, (h - StartHour) * HourPx, 1, 1));
                AbsoluteLayout.SetLayoutFlags(line,
                    AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.XProportional);
                col.Children.Add(line);
            }

            foreach (var b in blocks.Where(b => b.DayIndex == i))
            {
                var bg = MemberPalette.Resolve(b.Member, MemberPalette.Slot.Bg);
                var dot = MemberPalette.Resolve(b.Member, MemberPalette.Slot.Dot);
                var text = MemberPalette.Resolve(b.Member, MemberPalette.Slot.Text);

                var top = (b.StartHour - StartHour) * HourPx + 1;
                var height = Math.Max(b.DurationHours * HourPx - 2, 14);

                var stripe = new BoxView { BackgroundColor = dot, WidthRequest = 2 };
                AbsoluteLayout.SetLayoutBounds(stripe, new Rect(0, 0, 2, 1));
                AbsoluteLayout.SetLayoutFlags(stripe, AbsoluteLayoutFlags.HeightProportional);

                var label = new Label
                {
                    Text = b.Title,
                    FontFamily = "Geist",
                    FontSize = 8,
                    FontAttributes = FontAttributes.Bold,
                    LineHeight = 1.1,
                    TextColor = text,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    Margin = new Thickness(5, 2, 3, 2)
                };
                AbsoluteLayout.SetLayoutBounds(label, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(label,
                    AbsoluteLayoutFlags.SizeProportional | AbsoluteLayoutFlags.PositionProportional);

                var block = new Border
                {
                    StrokeThickness = 0,
                    BackgroundColor = bg,
                    StrokeShape = new RoundRectangle { CornerRadius = 4 },
                    Padding = 0,
                    Content = new Grid { Children = { stripe, label } }
                };
                AbsoluteLayout.SetLayoutBounds(block,
                    new Rect(0.02, top, 0.96, height));
                AbsoluteLayout.SetLayoutFlags(block, AbsoluteLayoutFlags.WidthProportional);
                col.Children.Add(block);
            }

            if (day?.IsToday == true)
            {
                var nowHour = DateTime.Now.Hour + DateTime.Now.Minute / 60.0;
                if (nowHour >= StartHour && nowHour <= EndHour)
                {
                    var nowLine = new BoxView { BackgroundColor = accent, HeightRequest = 2 };
                    AbsoluteLayout.SetLayoutBounds(nowLine, new Rect(0, (nowHour - StartHour) * HourPx, 1, 2));
                    AbsoluteLayout.SetLayoutFlags(nowLine, AbsoluteLayoutFlags.WidthProportional);
                    col.Children.Add(nowLine);

                    var nowDot = new Border
                    {
                        WidthRequest = 8,
                        HeightRequest = 8,
                        BackgroundColor = accent,
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 4 }
                    };
                    AbsoluteLayout.SetLayoutBounds(nowDot,
                        new Rect(0, (nowHour - StartHour) * HourPx - 3, 8, 8));
                    AbsoluteLayout.SetLayoutFlags(nowDot, AbsoluteLayoutFlags.None);
                    col.Children.Add(nowDot);
                }
            }

            // round day column corners with a clipping border wrapper
            var wrapper = new Border
            {
                StrokeThickness = 0,
                Padding = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Content = col
            };
            Grid.SetColumn(wrapper, i + 1);
            timelineRow.Children.Add(wrapper);
        }
    }
}
