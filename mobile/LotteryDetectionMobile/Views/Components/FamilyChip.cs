using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Pill chip used for priority / category / member labels.
///     <see cref="ChipColor" /> accepts: cream, accent, primary, high, med, low, alex, sam, jordan, riley, home.
/// </summary>
public sealed class FamilyChip : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FamilyChip), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty ChipColorProperty =
        BindableProperty.Create(nameof(ChipColor), typeof(string), typeof(FamilyChip), "cream",
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(FamilyChip), null,
            propertyChanged: OnVisualChanged);

    private readonly Border border;
    private readonly Label icon;
    private readonly Label label;

    public FamilyChip()
    {
        icon = new Label
        {
            FontFamily = "Geist",
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        label = new Label
        {
            FontFamily = "Geist",
            FontSize = 11.5,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 0.01,
            VerticalOptions = LayoutOptions.Center
        };
        var stack = new HorizontalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children = { icon, label }
        };
        border = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(9, 0),
            HeightRequest = 22,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content = stack
        };
        Content = border;

        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => Apply();

        Apply();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string ChipColor
    {
        get => (string)GetValue(ChipColorProperty);
        set => SetValue(ChipColorProperty, value);
    }

    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FamilyChip c) c.Apply();
    }

    private void Apply()
    {
        label.Text = Text ?? string.Empty;

        if (string.IsNullOrEmpty(Icon))
        {
            icon.IsVisible = false;
        }
        else
        {
            icon.IsVisible = true;
            icon.Text = Icon;
        }

        var (bg, fg) = ResolvePalette(ChipColor);
        border.BackgroundColor = bg;
        label.TextColor = fg;
        icon.TextColor = fg;
    }

    private static (Color bg, Color fg) ResolvePalette(string color)
    {
        switch ((color ?? "cream").ToLowerInvariant())
        {
            case "primary":
                return (
                    ResourceLookup.Color("FamilyPrimaryTintLight", "FamilyPrimaryTintDark", Color.FromArgb("#E0EAFF")),
                    ResourceLookup.Color("FamilyPrimaryLight", "FamilyPrimaryDark", Color.FromArgb("#1E5BFF")));
            case "accent":
                return (
                    ResourceLookup.Color("FamilyAccent2Light", "FamilyAccent2Dark", Color.FromArgb("#DBEAFE")),
                    ResourceLookup.Color("FamilyAccentLight", "FamilyAccentDark", Color.FromArgb("#3B82F6")));
            case "high":
                return (
                    ResourceLookup.Color("FamilyPriHighBgLight", "FamilyPriHighBgDark", Color.FromArgb("#DCEAFF")),
                    ResourceLookup.Color("FamilyPriHighTextLight", "FamilyPriHighTextDark", Color.FromArgb("#1E40AF")));
            case "med":
            case "medium":
                return (
                    ResourceLookup.Color("FamilyPriMedBgLight", "FamilyPriMedBgDark", Color.FromArgb("#E0EAFF")),
                    ResourceLookup.Color("FamilyPriMedTextLight", "FamilyPriMedTextDark", Color.FromArgb("#3730A3")));
            case "low":
                return (
                    ResourceLookup.Color("FamilyPriLowBgLight", "FamilyPriLowBgDark", Color.FromArgb("#E5E7EF")),
                    ResourceLookup.Color("FamilyPriLowTextLight", "FamilyPriLowTextDark", Color.FromArgb("#334155")));
            default:
                // Unknown string → try as member ID (GUID or legacy name) for hash-stable color
                if (!string.IsNullOrWhiteSpace(color))
                    return (MemberPalette.Resolve(color, MemberPalette.Slot.Bg),
                        MemberPalette.Resolve(color, MemberPalette.Slot.Text));
                return (
                    ResourceLookup.Color("FamilyCream2Light", "FamilyCream2Dark", Color.FromArgb("#E8F0FB")),
                    ResourceLookup.Color("FamilyInk2Light", "FamilyInk2Dark", Color.FromArgb("#1F2D44")));
        }
    }
}
