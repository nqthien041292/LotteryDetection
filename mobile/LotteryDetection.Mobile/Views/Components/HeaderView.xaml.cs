using System.Diagnostics;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Dialogs;
using LotteryDetection.Mobile.Services.Navigation;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetection.Mobile.Views.Components;

public partial class HeaderView : ContentView
{
    public static readonly BindableProperty ShowBackButtonProperty =
        BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(HeaderView), false);

    public static readonly BindableProperty HeaderTitleProperty =
        BindableProperty.Create(nameof(HeaderTitle), typeof(string), typeof(HeaderView), string.Empty);

    public static readonly BindableProperty ShowMenuButtonProperty =
        BindableProperty.Create(nameof(ShowMenuButton), typeof(bool), typeof(HeaderView), true);

    private bool _menuOpen;

    private Grid? _overlayGrid;

    public HeaderView()
    {
        InitializeComponent();
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public string HeaderTitle
    {
        get => (string)GetValue(HeaderTitleProperty);
        set => SetValue(HeaderTitleProperty, value);
    }

    public bool ShowMenuButton
    {
        get => (bool)GetValue(ShowMenuButtonProperty);
        set => SetValue(ShowMenuButtonProperty, value);
    }

    public bool HasTitle => !string.IsNullOrWhiteSpace(HeaderTitle);
    public event EventHandler? BackClicked;
    public event EventHandler? MenuClicked;

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent == null)
            DismissMenu();
    }

    private void OnBackButtonClicked(object? sender, TappedEventArgs e)
    {
        BackClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnMenuButtonClicked(object? sender, TappedEventArgs e)
    {
        MenuClicked?.Invoke(this, EventArgs.Empty);

        if (_menuOpen)
        {
            DismissMenu();
            return;
        }

        ShowMenu();
    }

    private void ShowMenu()
    {
        // Find the root Grid of the parent page
        var page = GetParentPage() as ContentPage;
        if (page?.Content is not Grid pageGrid) return;

        DismissMenu();
        _menuOpen = true;

        // Calculate position: header height + safe area offset
        var headerBounds = Bounds;
        var topOffset = headerBounds.Bottom > 0 ? headerBounds.Bottom : 64;

        // Build overlay: transparent container for popup + dismiss tap target
        _overlayGrid = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star) },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star) },
            ZIndex = 9999,
            BackgroundColor = Colors.Transparent,
            InputTransparent = false,
            CascadeInputTransparent = false
        };

        // Dismiss layer - uses minimal alpha so iOS still registers taps
        var dismissLayer = new ContentView
        {
            BackgroundColor = Color.FromRgba(0, 0, 0, 1), // 1/255 alpha - invisible but touchable
            InputTransparent = false
        };
        var dismissTap = new TapGestureRecognizer();
        dismissTap.Tapped += (_, _) => DismissMenu();
        dismissLayer.GestureRecognizers.Add(dismissTap);
        _overlayGrid.Children.Add(dismissLayer);

        // Popup card
        var popup = BuildPopupCard();
        popup.VerticalOptions = LayoutOptions.Start;
        popup.HorizontalOptions = LayoutOptions.End;
        popup.Margin = new Thickness(0, topOffset, 16, 0);

        _overlayGrid.Children.Add(popup);

        // Span the overlay across all rows/columns of the page grid
        Grid.SetRowSpan(_overlayGrid, Math.Max(pageGrid.RowDefinitions.Count, 1));
        Grid.SetColumnSpan(_overlayGrid, Math.Max(pageGrid.ColumnDefinitions.Count, 1));

        pageGrid.Children.Add(_overlayGrid);
    }

    private Border BuildPopupCard()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        var bgColor = GetDynamicColor("FamilySurface", isDark);
        var borderColor = GetDynamicColor("FamilyBorder", isDark);
        var textPrimary = GetDynamicColor("FamilyTextPrimary", isDark);
        var textSecondary = GetDynamicColor("FamilyTextSecondary", isDark);
        var dangerColor = GetDynamicColor("FamilyDanger", isDark);

        var fontFamily = "PlusJakartaSansVariable";
        try
        {
            if (Application.Current?.Resources.TryGetValue("FamilyFontFamily", out var ff) == true && ff is string f)
                fontFamily = f;
        }
        catch
        {
            /* use default */
        }

        var stack = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(4), WidthRequest = 210 };

        // Help & Support
        stack.Children.Add(BuildMenuItem("?", "Help & Support", textSecondary, textPrimary, fontFamily, OnHelpClicked));
        stack.Children.Add(BuildDivider(borderColor));

        // Sign out
        stack.Children.Add(BuildMenuItem("\u2192", "Sign out", dangerColor, dangerColor, fontFamily, OnSignOutClicked));

        var border = new Border
        {
            Content = stack,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = new SolidColorBrush(borderColor),
            StrokeThickness = 1,
            BackgroundColor = bgColor,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#40000000")),
                Offset = new Point(0, 4),
                Radius = 12
            }
        };

        return border;
    }

    private static Grid BuildMenuItem(string iconGlyph, string label, Color iconColor, Color textColor,
        string fontFamily, EventHandler<TappedEventArgs> handler)
    {
        var grid = new Grid
        {
            Padding = new Thickness(14, 12),
            ColumnDefinitions = { new ColumnDefinition(28), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += handler;
        grid.GestureRecognizers.Add(tap);

        var icon = new Label
        {
            Text = iconGlyph,
            FontFamily = fontFamily,
            FontSize = 18,
            TextColor = iconColor,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var text = new Label
        {
            Text = label,
            FontFamily = fontFamily,
            FontSize = 15,
            TextColor = textColor,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.NoWrap
        };
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        return grid;
    }

    private static BoxView BuildDivider(Color color)
    {
        return new BoxView
        {
            HeightRequest = 1,
            Margin = new Thickness(14, 0),
            Color = color
        };
    }

    private Color GetDynamicColor(string baseName, bool isDark)
    {
        var suffix = isDark ? "Dark" : "Light";
        var key = $"{baseName}{suffix}";

        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color c)
            return c;

        // Fallbacks
        return baseName switch
        {
            "FamilySurface" => isDark ? Color.FromArgb("#1E1E1E") : Colors.White,
            "FamilyBorder" => isDark ? Color.FromArgb("#333333") : Color.FromArgb("#E5E7EB"),
            "FamilyTextPrimary" => isDark ? Colors.White : Color.FromArgb("#1F2937"),
            "FamilyTextSecondary" => isDark ? Color.FromArgb("#9CA3AF") : Color.FromArgb("#6B7280"),
            "FamilyDanger" => isDark ? Color.FromArgb("#F87171") : Color.FromArgb("#EF4444"),
            _ => Colors.Gray
        };
    }

    private void DismissMenu()
    {
        if (_overlayGrid?.Parent is Grid parent)
            parent.Children.Remove(_overlayGrid);

        _overlayGrid = null;
        _menuOpen = false;
    }

    private Page? GetParentPage()
    {
        Element? current = this;
        while (current != null)
        {
            if (current is Page page)
                return page;
            current = current.Parent;
        }

        return null;
    }

    private async void OnHelpClicked(object? sender, TappedEventArgs e)
    {
        DismissMenu();
        await NavigationService.Default.NavigateToHelpAsync();
    }


    private async void OnSignOutClicked(object? sender, TappedEventArgs e)
    {
        DismissMenu();

        var confirmed = await AppDialog.ShowConfirmAsync(
            title: "Sign out",
            message: "Are you sure you want to sign out?",
            acceptText: "Sign out",
            cancelText: "Cancel",
            danger: true,
            icon: "🚪",
            iconBackground: "#FEE2E2");
        if (!confirmed) return;

        try
        {
            var authService = IPlatformApplication.Current?.Services.GetService<IAuthService>();
            if (authService != null)
                await authService.SignOutAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Header] Sign-out error: {ex.Message}");
        }

        await NavigationService.Default.NavigateToLoginWithSocialAsync();
    }
}
