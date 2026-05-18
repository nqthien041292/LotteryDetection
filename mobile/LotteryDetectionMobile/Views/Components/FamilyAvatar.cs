using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Circular monogram avatar coloured per family member id (alex/sam/jordan/riley/home).
/// </summary>
public sealed class FamilyAvatar : ContentView
{
    public static readonly BindableProperty NameProperty =
        BindableProperty.Create(nameof(Name), typeof(string), typeof(FamilyAvatar), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty MemberProperty =
        BindableProperty.Create(nameof(Member), typeof(string), typeof(FamilyAvatar), "home",
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(FamilyAvatar), 32d,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty RingProperty =
        BindableProperty.Create(nameof(Ring), typeof(bool), typeof(FamilyAvatar), false,
            propertyChanged: OnVisualChanged);

    private readonly Ellipse circle;
    private readonly Label initial;
    private readonly Grid root;

    public FamilyAvatar()
    {
        circle = new Ellipse();
        initial = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontFamily = "Geist",
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };
        root = new Grid { Children = { circle, initial } };
        Content = root;

        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => Apply();

        Apply();
    }

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public string Member
    {
        get => (string)GetValue(MemberProperty);
        set => SetValue(MemberProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool Ring
    {
        get => (bool)GetValue(RingProperty);
        set => SetValue(RingProperty, value);
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FamilyAvatar a) a.Apply();
    }

    private void Apply()
    {
        var size = Math.Max(8, Size);
        circle.WidthRequest = size;
        circle.HeightRequest = size;
        WidthRequest = size + (Ring ? 4 : 0);
        HeightRequest = size + (Ring ? 4 : 0);

        var dot = MemberPalette.Resolve(Member, MemberPalette.Slot.Dot);
        circle.Fill = new SolidColorBrush(dot);

        if (Ring)
        {
            var cream = ResourceLookup.Color("FamilyCreamLight", "FamilyCreamDark", Color.FromArgb("#F4F8FE"));
            circle.Stroke = new SolidColorBrush(cream);
            circle.StrokeThickness = 2;
        }
        else
        {
            circle.Stroke = null;
            circle.StrokeThickness = 0;
        }

        initial.FontSize = size * 0.42;
        initial.Text = string.IsNullOrEmpty(Name) ? string.Empty : Name[..1].ToUpperInvariant();
    }
}
