using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Notifications;
using Castle.Core.Logging;
using LotteryDetection.Authorization.Users;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace LotteryDetection.Notifications;

public class FirebaseRealTimeNotifier : IRealTimeNotifier, ITransientDependency
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<UserDeviceToken, long> _deviceTokenRepository;

    public FirebaseRealTimeNotifier(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<UserDeviceToken, long> deviceTokenRepository)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _deviceTokenRepository = deviceTokenRepository;
        Logger = NullLogger.Instance;
        
        InitializeFirebase();
    }

    public ILogger Logger { get; set; }
    public bool UseOnlyIfRequestedAsTarget => false; // Send to push by default if possible

    private void InitializeFirebase()
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                // In a real production app, you would load from a service-account.json
                // For this demo/development environment, we'll try to use default credentials 
                // or just catch the error if not configured.
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.GetApplicationDefault(),
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("FirebaseAdmin SDK could not be initialized. Push notifications may not be sent. " + ex.Message);
        }
    }

    public async Task SendNotificationsAsync(UserNotification[] userNotifications)
    {
        var userNotificationsGroupedByTenant = userNotifications.GroupBy(un => un.TenantId);
        foreach (var userNotificationByTenant in userNotificationsGroupedByTenant)
        {
            using (_unitOfWorkManager.Current.SetTenantId(userNotificationByTenant.First().TenantId))
            {
                var allUserIds = userNotificationByTenant.Select(x => x.UserId).Distinct().ToList();
                
                // Get all device tokens for these users
                var deviceTokens = await _deviceTokenRepository.GetAll()
                    .Where(t => allUserIds.Contains(t.UserId))
                    .ToListAsync();

                if (!deviceTokens.Any())
                {
                    continue;
                }

                foreach (var userNotification in userNotificationByTenant)
                {
                    var userTokens = deviceTokens.Where(t => t.UserId == userNotification.UserId).Select(t => t.Token).ToList();
                    if (!userTokens.Any())
                    {
                        continue;
                    }

                    var messageText = userNotification.Notification.Data.Properties.ContainsKey("Message") 
                        ? userNotification.Notification.Data["Message"].ToString() 
                        : "Bạn có thông báo mới từ Lottery Detection";

                    try
                    {
                        if (FirebaseApp.DefaultInstance != null)
                        {
                            var message = new MulticastMessage()
                            {
                                Tokens = userTokens,
                                Notification = new Notification()
                                {
                                    Title = "Lottery Detection",
                                    Body = messageText
                                },
                                Data = new System.Collections.Generic.Dictionary<string, string>()
                                {
                                    { "notificationId", userNotification.Id.ToString() },
                                    { "ticketId", userNotification.Notification.Data.Properties.ContainsKey("ticketId") ? userNotification.Notification.Data["ticketId"].ToString() : "" }
                                }
                            };

                            var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                            Logger.Info($"Sent push notification to {response.SuccessCount} devices for user {userNotification.UserId}");
                        }
                        else
                        {
                            Logger.Warn($"Firebase not initialized. Would have sent: '{messageText}' to tokens: {string.Join(", ", userTokens)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error sending Firebase push notification: " + ex.Message, ex);
                    }
                }
            }
        }
    }
}
