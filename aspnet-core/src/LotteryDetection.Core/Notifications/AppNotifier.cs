using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.Localization;
using Abp.Notifications;
using LotteryDetection.Authorization.Users;
using LotteryDetection.MultiTenancy;

namespace LotteryDetection.Notifications;

public class AppNotifier : LotteryDetectionDomainServiceBase, IAppNotifier
{
    private readonly INotificationPublisher _notificationPublisher;

    public AppNotifier(INotificationPublisher notificationPublisher)
    {
        _notificationPublisher = notificationPublisher;
    }

    public async Task WelcomeToTheApplicationAsync(User user)
    {
        await _notificationPublisher.PublishAsync(
            AppNotificationNames.WelcomeToTheApplication,
            new MessageNotificationData(L("WelcomeToTheApplicationNotificationMessage")),
            severity: NotificationSeverity.Success,
            userIds: new[] { user.ToUserIdentifier() }
        );
    }

    public async Task NewUserRegisteredAsync(User user)
    {
        var notificationData = new LocalizableMessageNotificationData(
            new LocalizableString(
                "NewUserRegisteredNotificationMessage",
                LotteryDetectionConsts.LocalizationSourceName
            )
        );

        notificationData["userName"] = user.UserName;
        notificationData["emailAddress"] = user.EmailAddress;

        await _notificationPublisher.PublishAsync(AppNotificationNames.NewUserRegistered, notificationData,
            tenantIds: new[] { user.TenantId });
    }

    public async Task NewTenantRegisteredAsync(Tenant tenant)
    {
        var notificationData = new LocalizableMessageNotificationData(
            new LocalizableString(
                "NewTenantRegisteredNotificationMessage",
                LotteryDetectionConsts.LocalizationSourceName
            )
        );

        notificationData["tenancyName"] = tenant.TenancyName;
        await _notificationPublisher.PublishAsync(AppNotificationNames.NewTenantRegistered, notificationData);
    }

    public async Task GdprDataPrepared(UserIdentifier user, Guid binaryObjectId)
    {
        var notificationData = new LocalizableMessageNotificationData(
            new LocalizableString(
                "GdprDataPreparedNotificationMessage",
                LotteryDetectionConsts.LocalizationSourceName
            )
        );

        notificationData["binaryObjectId"] = binaryObjectId;

        await _notificationPublisher.PublishAsync(AppNotificationNames.GdprDataPrepared, notificationData,
            userIds: new[] { user });
    }

    //This is for test purposes
    public async Task SendMessageAsync(UserIdentifier user, string message,
        NotificationSeverity severity = NotificationSeverity.Info)
    {
        await _notificationPublisher.PublishAsync(
            AppNotificationNames.SimpleMessage,
            new MessageNotificationData(message),
            severity: severity,
            userIds: new[] { user }
        );
    }

    public async Task SendMessageAsync(string notificationName, string message, UserIdentifier[] userIds = null,
        NotificationSeverity severity = NotificationSeverity.Info)
    {
        var tenants = NotificationPublisher.AllTenants;
        await _notificationPublisher.PublishAsync(
            notificationName,
            new MessageNotificationData(message),
            severity: severity,
            userIds: userIds
        );
    }

    public Task SendMessageAsync(UserIdentifier user, LocalizableString localizableMessage,
        IDictionary<string, object> localizableMessageData = null,
        NotificationSeverity severity = NotificationSeverity.Info)
    {
        return SendNotificationAsync(AppNotificationNames.SimpleMessage, user, localizableMessage,
            localizableMessageData, severity);
    }

    public Task TenantsMovedToEdition(UserIdentifier user, string sourceEditionName, string targetEditionName)
    {
        return SendNotificationAsync(AppNotificationNames.TenantsMovedToEdition, user,
            new LocalizableString(
                "TenantsMovedToEditionNotificationMessage",
                LotteryDetectionConsts.LocalizationSourceName
            ),
            new Dictionary<string, object>
            {
                { "sourceEditionName", sourceEditionName },
                { "targetEditionName", targetEditionName }
            });
    }

    public Task SomeUsersCouldntBeImported(UserIdentifier user, string fileToken, string fileType, string fileName)
    {
        return SendNotificationAsync(AppNotificationNames.DownloadInvalidImportUsers, user,
            new LocalizableString(
                "ClickToSeeInvalidUsers",
                LotteryDetectionConsts.LocalizationSourceName
            ),
            new Dictionary<string, object>
            {
                { "fileToken", fileToken },
                { "fileType", fileType },
                { "fileName", fileName }
            });
    }

    public async Task SendMassNotificationAsync(string message, UserIdentifier[] userIds = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        Type[] targetNotifiers = null
    )
    {
        await _notificationPublisher.PublishAsync(
            AppNotificationNames.MassNotification,
            new MessageNotificationData(message),
            severity: severity,
            userIds: userIds,
            targetNotifiers: targetNotifiers
        );
    }

    protected async Task SendNotificationAsync(string notificationName, UserIdentifier user,
        LocalizableString localizableMessage, IDictionary<string, object> localizableMessageData = null,
        NotificationSeverity severity = NotificationSeverity.Info)
    {
        var notificationData = new LocalizableMessageNotificationData(localizableMessage);
        if (localizableMessageData != null)
            foreach (var pair in localizableMessageData)
                notificationData[pair.Key] = pair.Value;

        await _notificationPublisher.PublishAsync(notificationName, notificationData, severity: severity,
            userIds: new[] { user });
    }

    public Task<TResult> TenantsMovedToEdition<TResult>(UserIdentifier argsUser, int sourceEditionId,
        int targetEditionId)
    {
        throw new NotImplementedException();
    }

    public async Task LotteryResultFoundAsync(UserIdentifier user, Guid ticketId, bool isWinner, string matchedPrize, decimal? prizeAmount)
    {
        var message = isWinner 
            ? $"Chúc mừng! Vé số của bạn đã trúng {matchedPrize} với số tiền {prizeAmount:N0}đ."
            : "Rất tiếc vé số của bạn không trúng thưởng lần này. Chúc bạn may mắn lần sau!";

        await _notificationPublisher.PublishAsync(
            AppNotificationNames.LotteryResultAvailable,
            new MessageNotificationData(message),
            severity: isWinner ? NotificationSeverity.Success : NotificationSeverity.Info,
            userIds: new[] { user }
        );
    }
}