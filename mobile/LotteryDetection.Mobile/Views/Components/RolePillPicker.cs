using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetection.Mobile.Views.Components;

/// <summary>
///     Pill button showing current role; tap fires <see cref="Command" /> with <see cref="CommandParameter" />.
///     Page hosts a DisplayActionSheet on the command to change role.
/// </summary>
public sealed class RolePillPicker : ContentView
{
    public static readonly BindableProperty RoleProperty =
        BindableProperty.Create(nameof(Role), typeof(string), typeof(RolePillPicker), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(RolePillPicker));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(RolePillPicker));

    private readonly Border border;
    private readonly Label caret;
    private readonly Label label;

    // Fired when the pill is tapped; use instead of Command binding inside DataTemplates.
    public event EventHandler<object?>? RoleTapped;

    public RolePillPicker()
    {
        label = new Label
        {
            FontFamily = "Geist",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };
        caret = new Label
        {
            FontFamily = "Geist",
            FontSize = 10,
            Text = "▾",
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        var stack = new HorizontalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children = { label, caret }
        };
        border = new Border
        {
            StrokeThickness = 1,
            Padding = new Thickness(12, 6),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content = stack
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            RoleTapped?.Invoke(this, CommandParameter);
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        };
        border.GestureRecognizers.Add(tap);
        Content = border;

        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => Apply();

        Apply();
    }

    public string Role
    {
        get => (string)GetValue(RoleProperty);
        set => SetValue(RoleProperty, value);
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
        if (bindable is RolePillPicker p) p.Apply();
    }

    private void Apply()
    {
        label.Text = string.IsNullOrEmpty(Role) ? "Set role" : Role;

        var bg = ResourceLookup.Color("FamilyPrimaryTintLight", "FamilyPrimaryTintDark", Color.FromArgb("#E0EAFF"));
        var fg = ResourceLookup.Color("FamilyPrimaryLight", "FamilyPrimaryDark", Color.FromArgb("#1E5BFF"));
        var stroke = ResourceLookup.Color("FamilyBorderLight", "FamilyBorderDark", Color.FromArgb("#D5E1F2"));

        border.BackgroundColor = bg;
        border.Stroke = new SolidColorBrush(stroke);
        label.TextColor = fg;
        caret.TextColor = fg;
    }
}
