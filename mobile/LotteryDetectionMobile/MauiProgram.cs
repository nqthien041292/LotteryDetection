using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using LotteryDetectionMobile.Services.AIAssistant;
using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Calendar;
using LotteryDetectionMobile.Services.Dashboard;
using LotteryDetectionMobile.Services.Family;
using LotteryDetectionMobile.Services.Gamification;
using LotteryDetectionMobile.Services.Help;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.Services.Notifications;
using LotteryDetectionMobile.Services.Profile;
using LotteryDetectionMobile.Services.Tasks;
using LotteryDetectionMobile.Services.Voice;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Voice;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace LotteryDetectionMobile;

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
                // Family AI design system (per docs/design/) — Geist for UI, Geist Mono for tabular numerics.
                TryAddFont("Geist-Variable.ttf", "Geist");
                TryAddFont("GeistMono-Variable.ttf", "GeistMono");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Global press animation for all native Button elements.
        // ConditionalWeakTable prevents duplicate subscription when handler reconnects.
        var animatedButtons = new ConditionalWeakTable<Button, object>();
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping(nameof(Button.Text), (_, view) =>
        {
            if (view is not Button btn || !animatedButtons.TryAdd(btn, btn)) return;
            btn.Pressed += async (s, _) => { if (s is View v) await v.ScaleTo(0.94, 75, Easing.CubicOut); };
            btn.Released += async (s, _) => { if (s is View v) await v.ScaleTo(1.0, 150, Easing.SpringOut); };
        });

        // Register Authentication service (before Voice services)
        builder.Services.AddSingleton<IAuthService, EntraIdAuthService>();

        // Register platform-specific streaming audio recorder (must be before HybridVoiceService)
#if IOS
        builder.Services.AddSingleton<IStreamingAudioRecorder, StreamingAudioRecorder>();
#elif ANDROID
        builder.Services.AddSingleton<IStreamingAudioRecorder, StreamingAudioRecorder>();
#endif

#if IOS
        builder.Services.AddSingleton<IPlatformAudioRecorder, PlatformAudioRecorder>();
#elif ANDROID
        builder.Services.AddSingleton<IPlatformAudioRecorder, PlatformAudioRecorder>();
#endif
        // Register AI services
        // AI assistant: real API
        builder.Services.AddSingleton<IAIService>(sp =>
            new RemoteAIService(sp.GetRequiredService<VoiceApiOptions>()));

        // Register Voice services (now can inject IAuthService)
        builder.Services.AddSingleton<VoiceApiOptions>(sp =>
        {
            var authService = sp.GetService<IAuthService>();
            return new VoiceApiOptions(authService);
        });
        builder.Services.AddSingleton<HybridVoiceService>(sp =>
        {
            var options = sp.GetService<VoiceApiOptions>();
            var logger = sp.GetService<ILogger<HybridVoiceService>>();
            var streamingRecorder = sp.GetService<IStreamingAudioRecorder>();
            var recorder = sp.GetService<IPlatformAudioRecorder>();
            return new HybridVoiceService(options, logger, streamingRecorder, recorder);
        });
        builder.Services.AddSingleton<IVoiceService>(sp => sp.GetRequiredService<HybridVoiceService>());

        // Register Dashboard real-time service (depends on VoiceApiOptions for base URL + token)
        builder.Services.AddSingleton<IDashboardRealtimeService>(sp =>
            new SignalRDashboardService(sp.GetRequiredService<VoiceApiOptions>()));

        // Register Task service backed by the live API (replaces MockTaskService for real data)
        builder.Services.AddSingleton<ITaskService>(sp =>
            new RemoteTaskService(sp.GetRequiredService<VoiceApiOptions>()));

        // Notifications: real API
        builder.Services.AddSingleton<INotificationService>(sp =>
            new RemoteNotificationService(sp.GetRequiredService<VoiceApiOptions>()));

        // Gamification: real API
        builder.Services.AddSingleton<IGamificationService>(sp =>
            new RemoteGamificationService(sp.GetRequiredService<VoiceApiOptions>()));

        // Rewards: real API
        builder.Services.AddSingleton<IRewardService>(sp =>
            new RemoteRewardService(sp.GetRequiredService<VoiceApiOptions>()));

        // Family members: real API
        builder.Services.AddSingleton<IFamilyService>(sp =>
            new RemoteFamilyService(sp.GetRequiredService<VoiceApiOptions>()));

        // Shared member cache — 5-min TTL, one network call shared across all ViewModels
        builder.Services.AddSingleton<IFamilyMemberCache>(sp =>
            new FamilyMemberCache(sp.GetRequiredService<IFamilyService>()));

        // Profile: real API (current-user display name update)
        builder.Services.AddSingleton<IProfileService>(sp =>
            new RemoteProfileService(sp.GetRequiredService<VoiceApiOptions>()));

        // Calendar: real API
        builder.Services.AddSingleton<ICalendarService>(sp =>
            new RemoteCalendarService(sp.GetRequiredService<VoiceApiOptions>()));

        // Help ticket: real API
        builder.Services.AddSingleton<IHelpTicketService>(sp =>
            new RemoteHelpTicketService(sp.GetRequiredService<VoiceApiOptions>()));

        // Family audit log: real API
        builder.Services.AddSingleton<IFamilyAuditLogService>(sp =>
            new RemoteFamilyAuditLogService(sp.GetRequiredService<VoiceApiOptions>()));

        // Register ViewModels
        builder.Services.AddTransient<SplashViewModel>(sp =>
        {
            var authService = sp.GetRequiredService<IAuthService>();
            return new SplashViewModel(NavigationService.Default, authService);
        });
        builder.Services.AddTransient<LoginPageViewModel>(sp =>
        {
            var authService = sp.GetRequiredService<IAuthService>();
            return new LoginPageViewModel(NavigationService.Default, authService);
        });
        builder.Services.AddTransient<VoiceCaptureViewModel>(sp =>
        {
            var authService = sp.GetRequiredService<IAuthService>();
            var voiceService = sp.GetRequiredService<IVoiceService>();
            var aiService = sp.GetRequiredService<IAIService>();
            return new VoiceCaptureViewModel(authService, voiceService, aiService);
        });

        // Register Pages
        builder.Services.AddTransient<VoiceCapturePage>();

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

            // Format is: [Namespace].[FileName.ext]
            // If the file is placed in a folder, it would be [Namespace].[Folder].[FileName.ext]
            var resourceName = "LotteryDetectionMobile.SyncfusionLicense.txt";

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
