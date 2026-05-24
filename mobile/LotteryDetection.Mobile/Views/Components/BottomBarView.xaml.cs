using Microsoft.Maui.Controls.Shapes;
using ShapesPath = Microsoft.Maui.Controls.Shapes.Path;

namespace LotteryDetection.Mobile.Views.Components;

public partial class BottomBarView : ContentView
{
    private readonly string[] tabKeys = { "Home", "Mic", "Settings" };
    private CancellationTokenSource? micPulseCts;
    private string selectedTab = "Home";

    public BottomBarView()
    {
        InitializeComponent();
        UpdateTabVisuals();

        // Re-apply colors when theme changes
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged += (_, _) => UpdateTabVisuals();

        Loaded += (_, _) => StartMicPulse();
        Unloaded += (_, _) => StopMicPulse();
    }

    public string SelectedTab
    {
        get => selectedTab;
        set
        {
            if (tabKeys.Contains(value))
            {
                selectedTab = value;
                UpdateTabVisuals();
            }
        }
    }

    public event EventHandler<TabSelectedEventArgs>? TabSelected;

    private async void OnTabTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string key && tabKeys.Contains(key))
        {
            if (key == "Mic" && MicContainer != null)
            {
                try
                {
                    await MicContainer.ScaleTo(0.9, 80, Easing.CubicOut);
                    await MicContainer.ScaleTo(1.0, 100, Easing.CubicOut);
                }
                catch
                {
                    // Animation interrupted - ignore
                }
            }

            SelectedTab = key;
            TabSelected?.Invoke(this, new TabSelectedEventArgs(selectedTab));
        }
    }

    private void StartMicPulse()
    {
        StopMicPulse();
        micPulseCts = new CancellationTokenSource();
        _ = RunMicPulseAsync(micPulseCts.Token);
    }

    private void StopMicPulse()
    {
        micPulseCts?.Cancel();
        micPulseCts?.Dispose();
        micPulseCts = null;
        if (MicContainer != null)
            MicContainer.Scale = 1.0;
    }

    private async Task RunMicPulseAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && MicContainer != null)
        {
            try
            {
                var fabUp = MicContainer.ScaleTo(1.08, 750, Easing.SinInOut);
                var haloUp = MicHalo?.FadeTo(0.6, 750, Easing.SinInOut) ?? Task.FromResult(true);
                var haloScaleUp = MicHalo?.ScaleTo(1.1, 750, Easing.SinInOut) ?? Task.FromResult(true);
                await Task.WhenAll(fabUp, haloUp, haloScaleUp);
                if (token.IsCancellationRequested) break;

                var fabDown = MicContainer.ScaleTo(1.0, 750, Easing.SinInOut);
                var haloDown = MicHalo?.FadeTo(0.3, 750, Easing.SinInOut) ?? Task.FromResult(true);
                var haloScaleDown = MicHalo?.ScaleTo(1.0, 750, Easing.SinInOut) ?? Task.FromResult(true);
                await Task.WhenAll(fabDown, haloDown, haloScaleDown);
                if (token.IsCancellationRequested) break;

                await Task.Delay(400, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Animation aborted by handler change - retry next loop
            }
        }

        if (MicContainer != null)
            MicContainer.Scale = 1.0;
        if (MicHalo != null)
        {
            MicHalo.Opacity = 0.35;
            MicHalo.Scale = 1.0;
        }
    }

    private void UpdateTabVisuals()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var primary = GetColor(isDark ? "FamilyPrimaryDark" : "FamilyPrimaryLight", Colors.Blue);
        var inactive = Color.FromArgb("#94A3B8");

        SetPathStrokeTabState(HomeIcon, HomeLabel, selectedTab == "Home", primary, inactive);
        SetMicState(selectedTab == "Mic", primary, inactive);
        SetPathStrokeTabState(ProfileIcon, ProfileLabel, selectedTab == "Settings", primary, inactive);

        UpdateActiveIndicator();
    }

    private void SetPathStrokeTabState(ShapesPath icon, Label label, bool isSelected, Color primary, Color text)
    {
        if (icon == null || label == null) return;
        icon.Stroke = new SolidColorBrush(isSelected ? primary : text);
        label.TextColor = isSelected ? primary : text;
    }

    private void SetMicState(bool isSelected, Color primary, Color text)
    {
        if (MicContainer == null || MicPath == null) return;
        MicContainer.Background = new SolidColorBrush(primary);
        MicPath.Fill = new SolidColorBrush(Colors.White);
    }

    private void UpdateActiveIndicator()
    {
        // Map tab keys to indicator elements
        var indicators = new Dictionary<string, BoxView?>
        {
            ["Home"] = HomeIndicator,
            ["Mic"] = null, // FAB has no underline indicator
            ["Settings"] = SettingsIndicator
        };

        foreach (var (key, indicator) in indicators)
            if (indicator != null)
                indicator.IsVisible = key == selectedTab;
    }

    private Color GetColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
            return color;

        return fallback;
    }
}