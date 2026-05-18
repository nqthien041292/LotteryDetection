using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Home dashboard quick-launch tile: tinted icon square + label + meta line
///     (bundle screens-home.jsx → QuickLaunch).
/// </summary>
public sealed class QuickLaunch : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(QuickLaunch), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(QuickLaunch), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty MetaProperty =
        BindableProperty.Create(nameof(Meta), typeof(string), typeof(QuickLaunch), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty AccentProperty =
        BindableProperty.Create(nameof(Accent), typeof(Color), typeof(QuickLaunch), Color.FromArgb("#1E5BFF"),
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty TintProperty =
        BindableProperty.Create(nameof(Tint), typeof(Color), typeof(QuickLaunch), Color.FromArgb("#E0EAFF"),
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(QuickLaunch));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(QuickLaunch));

    private readonly Border card;
    private readonly Border iconBadge;
    private readonly VectorIcon iconGlyph;
    private readonly Label labelText;
    private readonly Label metaText;

    public QuickLaunch()
    {
        iconGlyph = new VectorIcon
        {
            Size = 18,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        iconBadge = new Border
        {
            WidthRequest = 36,
            HeightRequest = 36,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            HorizontalOptions = LayoutOptions.Start,
            Content = iconGlyph
        };

        labelText = new Label
        {
            FontFamily = "Geist",
            FontSize = 13.5,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = -0.005
        };
        metaText = new Label
        {
            FontFamily = "Geist",
            FontSize = 11,
            Margin = new Thickness(0, 1, 0, 0)
        };

        var labelStack = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { labelText, metaText }
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(12),
            Children = { iconBadge, labelStack }
        };

        card = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = 0,
            Content = stack
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

    /// <summary>VectorIcon glyph key (e.g. board, sparkle, chat, calendar).</summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Meta
    {
        get => (string)GetValue(MetaProperty);
        set => SetValue(MetaProperty, value);
    }

    public Color Accent
    {
        get => (Color)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public Color Tint
    {
        get => (Color)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
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
        if (bindable is QuickLaunch q) q.Apply();
    }

    private void Apply()
    {
        iconGlyph.Glyph = Icon ?? string.Empty;
        iconGlyph.Color = Accent;
        iconBadge.BackgroundColor = Tint;

        labelText.Text = Label ?? string.Empty;
        metaText.Text = Meta ?? string.Empty;

        var paper = ResourceLookup.Color("FamilyPaperLight", "FamilyPaperDark", Colors.White);
        var border = ResourceLookup.Color("FamilyCream3Light", "FamilyCream3Dark", Color.FromArgb("#D5E1F2"));
        var ink = ResourceLookup.Color("FamilyInkLight", "FamilyInkDark", Color.FromArgb("#0A1628"));
        var ink3 = ResourceLookup.Color("FamilyInk3Light", "FamilyInk3Dark", Color.FromArgb("#5C6E8A"));

        card.BackgroundColor = paper;
        card.Stroke = new SolidColorBrush(border);
        labelText.TextColor = ink;
        metaText.TextColor = ink3;
    }
}
