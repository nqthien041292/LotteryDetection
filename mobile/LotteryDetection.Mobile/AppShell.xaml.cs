using LotteryDetection.Mobile.Views.Dashboard;
using LotteryDetection.Mobile.Views.Family;
using LotteryDetection.Mobile.Views.Forms;
using LotteryDetection.Mobile.Views.LotteryCapture;
using LotteryDetection.Mobile.Views.LotteryHistory;
using LotteryDetection.Mobile.Views.LotteryResults;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetection.Mobile;

public partial class AppShell : Shell
{
    // Overlay added to the source page's root Grid during navigation transitions.
    // Provides immediate visual feedback while Shell constructs the destination page.
    private Grid? navOverlay;
    private int navOverlayVersion;

    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();

        Navigating += OnShellNavigating;
        Navigated += OnShellNavigated;

        // Startup routing is owned by SplashPage (the first ShellContent),
        // which resolves onboarding/session state then navigates.
    }

    // ── navigation overlay ──────────────────────────────────────────────────

    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        // Fires on main thread at the very start of GoToAsync, before the
        // destination page is constructed — this is the "frozen" gap.
        RemoveNavOverlay();
        ShowNavOverlay();
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        RemoveNavOverlay();
    }

    private void ShowNavOverlay()
    {
        // Only overlay pages whose root content is a Grid (all our ContentPages).
        if (CurrentPage is not ContentPage contentPage) return;
        if (contentPage is SplashPage) return; // Do not show global loading overlay on the SplashPage
        if (contentPage.Content is not Grid rootGrid) return;

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            StrokeThickness = 0,
            Padding = new Thickness(24),
            BackgroundColor = isDark ? Color.FromArgb("#1F2D44") : Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 8),
                Radius = 24,
                Opacity = 0.14f
            },
            Content = new ActivityIndicator
            {
                IsRunning = true,
                Color = Color.FromArgb("#1E5BFF"),
                WidthRequest = 32,
                HeightRequest = 32,
            }
        };

        navOverlay = new Grid
        {
            BackgroundColor = Color.FromArgb("#26000000"), // 15 % dim scrim
            InputTransparent = false,
        };
        Grid.SetRowSpan(navOverlay, 10);
        Grid.SetColumnSpan(navOverlay, 10);
        navOverlay.Add(card);

        rootGrid.Add(navOverlay);

        var version = ++navOverlayVersion;
        _ = RemoveNavOverlayAfterDelayAsync(version);
    }

    private void RemoveNavOverlay()
    {
        if (navOverlay?.Parent is Grid parent)
            parent.Remove(navOverlay);
        navOverlay = null;
        navOverlayVersion++;
    }

    private async Task RemoveNavOverlayAfterDelayAsync(int version)
    {
        await Task.Delay(5000);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (version == navOverlayVersion)
                RemoveNavOverlay();
        });
    }

    // ── route registration ──────────────────────────────────────────────────

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(SplashPage), typeof(SplashPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
        Routing.RegisterRoute(nameof(LotteryCapturePage), typeof(LotteryCapturePage));
        Routing.RegisterRoute(nameof(LotteryResultsPage), typeof(LotteryResultsPage));
        Routing.RegisterRoute(nameof(LotteryLiveResultsPage), typeof(LotteryLiveResultsPage));
        Routing.RegisterRoute(nameof(LotteryHistoryPage), typeof(LotteryHistoryPage));
        Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
    }
}
