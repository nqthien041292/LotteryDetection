using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using LotteryDetection.Mobile.Models.Calendar;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetection.Mobile.Views.Components;

/// <summary>
///     Bundle screens-calendar.jsx → MonthGrid. 7-col grid card with day-of-week header,
///     today filled-ink circle, selected cell on primary tint, and up-to-three member dots.
/// </summary>
public sealed class CalendarMonthGrid : ContentView
{
    public static readonly BindableProperty CellsProperty =
        BindableProperty.Create(nameof(Cells), typeof(IEnumerable), typeof(CalendarMonthGrid),
            propertyChanged: OnCellsChanged);

    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(CalendarMonthGrid));

    private static readonly string[] DowLabels = { "M", "T", "W", "T", "F", "S", "S" };

    private readonly Border card;
    private readonly Grid grid;

    public CalendarMonthGrid()
    {
        var dowRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        for (var i = 0; i < 7; i++) dowRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
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
            Grid.SetColumn(lbl, i);
            dowRow.Children.Add(lbl);
        }

        grid = new Grid { RowSpacing = 2, ColumnSpacing = 0 };
        for (var i = 0; i < 7; i++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(8, 12, 8, 12),
            Spacing = 0,
            Children = { dowRow, grid }
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

    public IEnumerable? Cells
    {
        get => (IEnumerable?)GetValue(CellsProperty);
        set => SetValue(CellsProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    private static void OnCellsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not CalendarMonthGrid g) return;
        if (oldValue is INotifyCollectionChanged oldNcc) oldNcc.CollectionChanged -= g.OnCellsCollectionChanged;
        if (newValue is INotifyCollectionChanged newNcc) newNcc.CollectionChanged += g.OnCellsCollectionChanged;
        g.Rebuild();
    }

    private void OnCellsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

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
        grid.Children.Clear();
        grid.RowDefinitions.Clear();

        if (Cells == null) return;

        var cells = Cells.Cast<MonthCell>().ToList();
        var rows = (int)Math.Ceiling(cells.Count / 7.0);
        for (var r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(50));

        var ink = ResourceLookup.Color("FamilyInkLight", "FamilyInkDark", Color.FromArgb("#0A1628"));
        var ink4 = ResourceLookup.Color("FamilyInk4Light", "FamilyInk4Dark", Color.FromArgb("#94A3BE"));
        var primary = ResourceLookup.Color("FamilyPrimaryLight", "FamilyPrimaryDark", Color.FromArgb("#1E5BFF"));
        var primary3 = ResourceLookup.Color("FamilyPrimaryTintLight", "FamilyPrimaryTintDark", Color.FromArgb("#E5EDFF"));

        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            var row = i / 7;
            var col = i % 7;

            if (cell.Day == null)
            {
                var pad = new BoxView { BackgroundColor = Colors.Transparent, HeightRequest = 50 };
                Grid.SetRow(pad, row);
                Grid.SetColumn(pad, col);
                grid.Children.Add(pad);
                continue;
            }

            var dayCircle = new Border
            {
                WidthRequest = 24,
                HeightRequest = 24,
                StrokeThickness = 0,
                BackgroundColor = cell.IsToday ? ink : Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = cell.Day.ToString(),
                    FontFamily = "Geist",
                    FontSize = 12.5,
                    FontAttributes = (cell.IsToday || cell.IsSelected) ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = cell.IsToday ? Colors.White : (cell.IsSelected ? primary : ink),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };

            var dotRow = new HorizontalStackLayout
            {
                Spacing = 1.5,
                HeightRequest = 5,
                HorizontalOptions = LayoutOptions.Center
            };
            foreach (var member in cell.MemberDots.Take(3))
            {
                dotRow.Children.Add(new BoxView
                {
                    WidthRequest = 4,
                    HeightRequest = 4,
                    CornerRadius = 2,
                    BackgroundColor = MemberPalette.Resolve(member, MemberPalette.Slot.Dot)
                });
            }
            if (cell.OverflowCount > 0)
            {
                dotRow.Children.Add(new Label
                {
                    Text = "+",
                    FontFamily = "GeistMono",
                    FontSize = 7,
                    TextColor = ink4,
                    LineHeight = 1
                });
            }

            var inner = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { dayCircle, dotRow }
            };

            // Highlight is sized to its content and centered, so it stays
            // concentric with the day circle and cannot drift within the cell.
            var highlight = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = cell.IsSelected ? primary3 : Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(9, 6),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = inner
            };

            // Keep the tap host a transparent Border (a View) — the original,
            // proven-tappable pattern here. A transparent Grid/Layout has iOS
            // hit-test gaps in empty regions.
            var cellHost = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = 0,
                HeightRequest = 50,
                Content = highlight
            };

            var capturedCell = cell;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (SelectCommand?.CanExecute(capturedCell) == true)
                    SelectCommand.Execute(capturedCell);
            };
            cellHost.GestureRecognizers.Add(tap);

            Grid.SetRow(cellHost, row);
            Grid.SetColumn(cellHost, col);
            grid.Children.Add(cellHost);
        }
    }
}
