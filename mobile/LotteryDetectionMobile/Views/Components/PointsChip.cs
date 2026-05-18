using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Tiny "+30 XP" reward chip displayed on task cards. Bound to a priority string (high/med/low).
/// </summary>
public sealed class PointsChip : ContentView
{
    public static readonly BindableProperty PriorityProperty =
        BindableProperty.Create(nameof(Priority), typeof(string), typeof(PointsChip), "med",
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty PointsProperty =
        BindableProperty.Create(nameof(Points), typeof(int), typeof(PointsChip), 0,
            propertyChanged: OnVisualChanged);

    private readonly Border border;
    private readonly Label label;

    public PointsChip()
    {
        label = new Label
        {
            FontFamily = "GeistMono",
            FontSize = 10.5,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = -0.01,
            VerticalOptions = LayoutOptions.Center
        };
        var stack = new HorizontalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "⚡", FontSize = 10, VerticalOptions = LayoutOptions.Center },
                label
            }
        };
        border = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(7, 0),
            HeightRequest = 20,
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

    public string Priority
    {
        get => (string)GetValue(PriorityProperty);
        set => SetValue(PriorityProperty, value);
    }

    public int Points
    {
        get => (int)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PointsChip c) c.Apply();
    }

    private void Apply()
    {
        var pts = Points > 0 ? Points : DefaultPointsFor(Priority);
        label.Text = $"+{pts}";

        var primary = ResourceLookup.Color("FamilyPrimaryLight", "FamilyPrimaryDark", Color.FromArgb("#1E5BFF"));
        var bg = primary.WithAlpha(0.10f);
        border.BackgroundColor = bg;
        label.TextColor = primary;
    }

    public static int DefaultPointsFor(string? priority)
    {
        return (priority ?? "med").ToLowerInvariant() switch
        {
            "high" => 30,
            "low" => 10,
            _ => 20
        };
    }
}
