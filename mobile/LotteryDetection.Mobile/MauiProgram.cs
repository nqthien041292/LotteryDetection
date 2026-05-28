using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using LotteryDetection.Mobile.Services;
using LotteryDetection.Mobile.Services.Api;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Configuration;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.LotteryCapture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace LotteryDetection.Mobile;

public static class MauiProgram
{
    public static IServiceProvider? Services { get; private set; }

    public static MauiApp CreateMauiApp()
    {
        // Force en-US for all date/time/number formatting regardless of device locale.
        // Keeps XAML StringFormat patterns ('MMM d', 'h:mm tt', etc.) consistent and
        // prevents Vietnamese month names from leaking through on VN-locale devices.
        var enUs = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = enUs;
        CultureInfo.DefaultThreadCurrentUICulture = enUs;
        Thread.CurrentThread.CurrentCulture = enUs;
        Thread.CurrentThread.CurrentUICulture = enUs;

        // 1. Load the license from the text file
        RegisterSyncfusionLicense();

        var builder = MauiApp.CreateBuilder();
        builder
            .ConfigureSyncfusionCore()
            .ConfigureSyncfusionToolkit()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Wrap each registration so a single missing/invalid TTF can't crash launch.
                // On iOS, MAUI delegates to CTFontManagerRegisterFontsForURL which can throw
                // for missing assets — without this guard the whole MauiApp.Build() aborts.
                void TryAddFont(string file, string alias)
                {
                    try { fonts.AddFont(file, alias); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Fonts] Skipped '{file}' ({alias}): {ex.Message}");
                    }
                }

                TryAddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                TryAddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                TryAddFont("Roboto-Medium.ttf", "Roboto-Medium");
                TryAddFont("Roboto-Regular.ttf", "Roboto-Regular");
                TryAddFont("Righteous-Regular.ttf", "Righteous");
                TryAddFont("UIFontIcons.ttf", "FontIcons");
                TryAddFont("Dashboard.ttf", "DashboardFontIcons");
                TryAddFont("PlusJakartaSans-Variable.ttf", "PlusJakartaSans");
                TryAddFont("Geist-Variable.ttf", "Geist");
                TryAddFont("GeistMono-Variable.ttf", "GeistMono");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Global press animation for all native Button elements.
        // Uses PointerGestureRecognizer for consistency with Border below: Button.Pressed/Released
        // fire inconsistently on MAUI 9 iOS handlers, while PointerPressed/Released are reliable.
        // ConditionalWeakTable prevents duplicate subscription when handler reconnects.
        var animatedButtons = new ConditionalWeakTable<Button, object>();
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping(nameof(Button.Text), (_, view) =>
        {
            if (view is not Button btn || !animatedButtons.TryAdd(btn, btn)) return;
            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += async (s, _) =>
            {
                if (s is View v) await v.ScaleTo(0.94, 90, Easing.CubicOut);
            };
            pointer.PointerReleased += async (s, _) =>
            {
                if (s is View v) await v.ScaleTo(1.0, 180, Easing.SpringOut);
            };
            btn.GestureRecognizers.Add(pointer);
        });

        // Same press animation for any Border that opts into taps via a TapGestureRecognizer
        // (Dashboard cards, capture zone, etc.). PointerPressed/Released were added in MAUI 9
        // and fire on iOS touch-down/up.
        var animatedBorders = new ConditionalWeakTable<Border, object>();
        Microsoft.Maui.Handlers.BorderHandler.Mapper.AppendToMapping(nameof(Border.Content), (_, view) =>
        {
            if (view is not Border border || !animatedBorders.TryAdd(border, border)) return;
            if (!border.GestureRecognizers.OfType<TapGestureRecognizer>().Any()) return;

            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += async (s, _) =>
            {
                if (s is View v) await v.ScaleTo(0.96, 75, Easing.CubicOut);
            };
            pointer.PointerReleased += async (s, _) =>
            {
                if (s is View v) await v.ScaleTo(1.0, 150, Easing.SpringOut);
            };
            border.GestureRecognizers.Add(pointer);
        });

#if IOS
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("TransparentDatePicker", (handler, view) =>
        {
            if (handler.PlatformView is UIKit.UITextField textField)
            {
                textField.BorderStyle = UIKit.UITextBorderStyle.None;
                textField.BackgroundColor = UIKit.UIColor.Clear;
                textField.TextColor = UIKit.UIColor.Clear;
                textField.TintColor = UIKit.UIColor.Clear;
                textField.UserInteractionEnabled = true;
            }
        });
#endif

        // When `Api.BaseUrl` is configured in appsettings, talk to the real backend
        // (LotteryDetection.Web.Host). Auth flips together with the lottery services
        // so the bearer token they attach is actually accepted by ABP.
        var backendBaseUrl = AppConfiguration.GetBackendApiBaseUrl();
        if (!string.IsNullOrWhiteSpace(backendBaseUrl))
        {
            HttpClient NewBackendClient(int timeoutSeconds = 60)
            {
                var client = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false
                })
                {
                    BaseAddress = new Uri(backendBaseUrl, UriKind.Absolute),
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                };
                client.DefaultRequestHeaders.Add("X-App-Key", LotteryDetection.Mobile.Services.Configuration.SecureKeyHelper.GetDecryptedKey());
                return client;
            }

            builder.Services.AddSingleton<IAuthService>(_ => new ApiAbpAuthService(NewBackendClient(30)));
            builder.Services.AddSingleton<ILotteryDetectionService>(sp =>
                new ApiLotteryDetectionService(NewBackendClient(60), sp.GetRequiredService<IAuthService>()));
            builder.Services.AddSingleton<ILotteryHistoryService>(sp =>
                new ApiLotteryHistoryService(NewBackendClient(30), sp.GetRequiredService<IAuthService>()));
            builder.Services.AddSingleton<ILotteryResultsService>(sp =>
                new Services.Api.ApiLotteryResultsService(NewBackendClient(30), sp.GetRequiredService<IAuthService>()));
        }
        else
        {
            builder.Services.AddSingleton<IAuthService>(_ => MockAuthService.Instance);
            builder.Services.AddSingleton<ILotteryDetectionService>(_ => MockLotteryDetectionService.Instance);
            builder.Services.AddSingleton<ILotteryHistoryService>(_ => MockLotteryHistoryService.Instance);
            builder.Services.AddSingleton<ILotteryResultsService>(_ => MockLotteryResultsService.Instance);
        }
        builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();

        // Register ViewModels
        builder.Services.AddTransient<SplashViewModel>(sp =>
        {
            var authService = sp.GetRequiredService<IAuthService>();
            var pushService = sp.GetRequiredService<IPushNotificationService>();
            return new SplashViewModel(NavigationService.Default, authService, pushService);
        });
        builder.Services.AddTransient<LoginPageViewModel>(sp =>
        {
            var authService = sp.GetRequiredService<IAuthService>();
            var pushService = sp.GetRequiredService<IPushNotificationService>();
            return new LoginPageViewModel(NavigationService.Default, authService, pushService);
        });
        builder.Services.AddTransient<LotteryCaptureViewModel>(sp =>
        {
            var detectionService = sp.GetRequiredService<ILotteryDetectionService>();
            return new LotteryCaptureViewModel(detectionService, NavigationService.Default);
        });

        // Register Pages
        builder.Services.AddTransient<LotteryCapturePage>();

        var app = builder.Build();
        Services = app.Services;
        return app;
    }

    /// <summary>
    ///     Reads the Syncfusion License from an Embedded Resource text file and registers it.
    /// </summary>
    private static void RegisterSyncfusionLicense()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "LotteryDetection.Mobile.SyncfusionLicense.txt";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.WriteLine($"WARNING: Embedded resource '{resourceName}' was not found.");
                return;
            }

            using var reader = new StreamReader(stream);
            var licenseKey = reader.ReadToEnd().Trim();

            // Real Syncfusion keys are >100 chars; an empty/placeholder file should not call
            // RegisterLicense (which can pop a license dialog that the splash kills on iOS).
            if (string.IsNullOrWhiteSpace(licenseKey) || licenseKey.Length <= 32)
            {
                Debug.WriteLine("[Syncfusion] License missing or too short — skipping registration.");
                return;
            }

            SyncfusionLicenseProvider.RegisterLicense(licenseKey);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load Syncfusion License: {ex.Message}");
        }
    }
}
