using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Home dashboard summary tile: large value (display font) + uppercase label,
///     with a tinted corner badge that mirrors the value (bundle screens-home.jsx → SummaryTile).
/// </summary>
public sealed class SummaryTile : ContentView
{
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(SummaryTile), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(SummaryTile), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty TintBgProperty =
        BindableProperty.Create(nameof(TintBg), typeof(Color), typeof(SummaryTile), Color.FromArgb("#FFE4DD"),
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty TintFgProperty =
        BindableProperty.Create(nameof(TintFg), typeof(Color), typeof(SummaryTile), Color.FromArgb("#B23A1A"),
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SummaryTile));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(SummaryTile));

    private readonly Label badgeLabel;
    private readonly Border badge;
    private readonly Border card;
    private readonly Label captionLabel;
    private readonly Label valueLabel;

    public SummaryTile()
    {
        badgeLabel = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        badge = new Border
        {
            WidthRequest = 22,
            HeightRequest = 22,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 },
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 8, 8, 0),
            Content = badgeLabel
        };

        valueLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = -0.025,
            LineHeight = 1,
            Margin = new Thickness(0, 2, 0, 0)
        };
        captionLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 10.5,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 0.04,
            TextTransform = TextTransform.Uppercase,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 0,
            Padding = new Thickness(12, 10, 12, 10),
            Children = { valueLabel, captionLabel }
        };

        var grid = new Grid { Children = { stack, badge } };

        card = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = 0,
            Content = grid
        };
        Content = card;

        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => Apply();

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            try
            {
                await card.ScaleTo(0.94, 75, Easing.CubicOut);
                await card.ScaleTo(1.0, 150, Easing.SpringOut);
            }
            catch { /* element unloaded */ }
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        };
        card.GestureRecognizers.Add(tap);

        Apply();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Color TintBg
    {
        get => (Color)GetValue(TintBgProperty);
        set => SetValue(TintBgProperty, value);
    }

    public Color TintFg
    {
        get => (Color)GetValue(TintFgProperty);
        set => SetValue(TintFgProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SummaryTile t) t.Apply();
    }

    private void Apply()
    {
        valueLabel.Text = Value ?? string.Empty;
        captionLabel.Text = Label ?? string.Empty;
        badgeLabel.Text = Value ?? string.Empty;
        badge.BackgroundColor = TintBg;
        badgeLabel.TextColor = TintFg;

        var paper = ResourceLookup.Color("FamilyPaperLight", "FamilyPaperDark", Colors.White);
        var border = ResourceLookup.Color("FamilyCream3Light", "FamilyCream3Dark", Color.FromArgb("#D5E1F2"));
        var ink = ResourceLookup.Color("FamilyInkLight", "FamilyInkDark", Color.FromArgb("#0A1628"));
        var ink3 = ResourceLookup.Color("FamilyInk3Light", "FamilyInk3Dark", Color.FromArgb("#5C6E8A"));

        card.BackgroundColor = paper;
        card.Stroke = new SolidColorBrush(border);
        valueLabel.TextColor = ink;
        captionLabel.TextColor = ink3;
    }
}
