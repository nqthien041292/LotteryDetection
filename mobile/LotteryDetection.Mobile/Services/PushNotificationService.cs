using System.Diagnostics;
using LotteryDetection.Mobile.Services.Interfaces;
#if IOS || ANDROID
using Plugin.Firebase.CloudMessaging;
#endif

namespace LotteryDetection.Mobile.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ILotteryDetectionService _lotteryDetectionService;

    public PushNotificationService(ILotteryDetectionService lotteryDetectionService)
    {
        _lotteryDetectionService = lotteryDetectionService;
    }

    public async Task InitializeAsync()
    {
        try
        {
#if IOS || ANDROID
            // Only attempt if Firebase is actually available/initialized
            // This is a safeguard if the developer forgot to add GoogleService-Info.plist
            if (Plugin.Firebase.CrossFirebase.IsInitialized)
            {
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                
                CrossFirebaseCloudMessaging.Current.NotificationReceived += (s, e) =>
                {
                    Debug.WriteLine($"[PushNotificationService] Notification Received: {e.Notification.Title} - {e.Notification.Body}");
                };

                CrossFirebaseCloudMessaging.Current.TokenChanged += async (s, e) =>
                {
                    Debug.WriteLine($"[PushNotificationService] Token Changed: {e.Token}");
                    await RegisterTokenAsync();
                };
            }
            else
            {
                Debug.WriteLine("[PushNotificationService] Skip initialization: Firebase not initialized.");
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PushNotificationService] Initialization failed: {ex.Message}");
        }
    }

    public async Task<string> GetTokenAsync()
    {
#if IOS || ANDROID
        try
        {
            return await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PushNotificationService] GetTokenAsync failed: {ex.Message}");
            return string.Empty;
        }
#else
        return string.Empty;
#endif
    }

    public async Task RegisterTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return;

        var deviceType = DeviceInfo.Platform.ToString().ToLower();
        var deviceName = DeviceInfo.Name;

        try
        {
            await _lotteryDetectionService.RegisterDeviceTokenAsync(token, deviceType, deviceName);
            Debug.WriteLine($"[PushNotificationService] Token registered successfully: {token}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PushNotificationService] RegisterTokenAsync failed: {ex.Message}");
        }
    }
}
