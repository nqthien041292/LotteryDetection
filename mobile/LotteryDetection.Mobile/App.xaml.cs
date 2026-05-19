using System.Diagnostics;

namespace LotteryDetection.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Restore saved dark mode preference before any window is created
        UserAppTheme = Preferences.Get("app_dark_mode", false) ? AppTheme.Dark : AppTheme.Light;

        // Prevent hard crashes from unhandled exceptions in async void methods
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Debug.WriteLine(
                $"[App] UnhandledException: {(e.ExceptionObject as Exception)?.Message}");
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Debug.WriteLine(
                $"[App] UnobservedTaskException: {e.Exception?.Message}");
            e.SetObserved();
        };
    }

    #region Properties

    public static string ImageServerPath { get; } =
        "https://cdn.syncfusion.com/essential-ui-kit-for-.net-maui/common/uikitimages/";

    #endregion

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
