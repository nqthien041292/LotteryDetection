using Abp.Application.Editions;
using Abp.Application.Features;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.DynamicEntityProperties;
using Abp.EntityHistory;
using Abp.Extensions;
using Abp.Localization;
using Abp.Notifications;
using Abp.Organizations;
using Abp.UI.Inputs;
using Abp.Webhooks;
using AutoMapper;
using LotteryDetection.Auditing.Dto;
using LotteryDetection.Authorization.Accounts.Dto;
using LotteryDetection.Authorization.Delegation;
using LotteryDetection.Authorization.Permissions.Dto;
using LotteryDetection.Authorization.Roles;
using LotteryDetection.Authorization.Roles.Dto;
using LotteryDetection.Authorization.Users;
using LotteryDetection.Authorization.Users.Delegation.Dto;
using LotteryDetection.Authorization.Users.Dto;
using LotteryDetection.Authorization.Users.Importing.Dto;
using LotteryDetection.Authorization.Users.Profile.Dto;
using LotteryDetection.Chat;
using LotteryDetection.Chat.Dto;
using LotteryDetection.Common.Dto;
using LotteryDetection.DynamicEntityProperties.Dto;
using LotteryDetection.Editions;
using LotteryDetection.Editions.Dto;
using LotteryDetection.EntityChanges;
using LotteryDetection.EntityChanges.Dto;
using LotteryDetection.Friendships;
using LotteryDetection.Friendships.Cache;
using LotteryDetection.Friendships.Dto;
using LotteryDetection.Localization.Dto;
using LotteryDetection.MultiTenancy;
using LotteryDetection.MultiTenancy.Dto;
using LotteryDetection.MultiTenancy.HostDashboard.Dto;
using LotteryDetection.MultiTenancy.Payments;
using LotteryDetection.MultiTenancy.Payments.Dto;
using LotteryDetection.Notifications.Dto;
using LotteryDetection.Organizations.Dto;
using LotteryDetection.Sessions.Dto;
using LotteryDetection.WebHooks.Dto;

namespace LotteryDetection;

internal static class CustomDtoMapper
{
    public static void CreateMappings(IMapperConfigurationExpression configuration)
    {
        //Inputs
        configuration.CreateMap<CheckboxInputType, FeatureInputTypeDto>();
        configuration.CreateMap<SingleLineStringInputType, FeatureInputTypeDto>();
        configuration.CreateMap<ComboboxInputType, FeatureInputTypeDto>();
        configuration.CreateMap<IInputType, FeatureInputTypeDto>()
            .Include<CheckboxInputType, FeatureInputTypeDto>()
            .Include<SingleLineStringInputType, FeatureInputTypeDto>()
            .Include<ComboboxInputType, FeatureInputTypeDto>();
        configuration.CreateMap<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>();
        configuration.CreateMap<ILocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>()
            .Include<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>();
        configuration.CreateMap<LocalizableComboboxItem, LocalizableComboboxItemDto>();
        configuration.CreateMap<ILocalizableComboboxItem, LocalizableComboboxItemDto>()
            .Include<LocalizableComboboxItem, LocalizableComboboxItemDto>();

        //Chat
        configuration.CreateMap<ChatMessage, ChatMessageDto>();
        configuration.CreateMap<ChatMessage, ChatMessageExportDto>();

        //Feature
        configuration.CreateMap<FlatFeatureSelectDto, Feature>().ReverseMap();
        configuration.CreateMap<Feature, FlatFeatureDto>();

        //Role
        configuration.CreateMap<RoleEditDto, Role>().ReverseMap();
        configuration.CreateMap<Role, RoleListDto>();
        configuration.CreateMap<UserRole, UserListRoleDto>();


        //Edition
        configuration.CreateMap<EditionEditDto, SubscribableEdition>().ReverseMap();
        configuration.CreateMap<EditionCreateDto, SubscribableEdition>();
        configuration.CreateMap<EditionSelectDto, SubscribableEdition>().ReverseMap();
        configuration.CreateMap<SubscribableEdition, EditionInfoDto>();

        configuration.CreateMap<Edition, EditionInfoDto>().Include<SubscribableEdition, EditionInfoDto>();

        configuration.CreateMap<SubscribableEdition, EditionListDto>();
        configuration.CreateMap<Edition, EditionEditDto>();
        configuration.CreateMap<Edition, SubscribableEdition>();
        configuration.CreateMap<Edition, EditionSelectDto>();


        //Payment
        configuration.CreateMap<SubscriptionPaymentDto, SubscriptionPayment>()
            .ReverseMap()
            .ForMember(dto => dto.TotalAmount, options => options.MapFrom(e => e.GetTotalAmount()));
        configuration.CreateMap<SubscriptionPaymentProductDto, SubscriptionPaymentProduct>().ReverseMap();
        configuration.CreateMap<SubscriptionPaymentListDto, SubscriptionPayment>().ReverseMap();
        configuration.CreateMap<SubscriptionPayment, SubscriptionPaymentInfoDto>();

        //Permission
        configuration.CreateMap<Permission, FlatPermissionDto>();
        configuration.CreateMap<Permission, FlatPermissionWithLevelDto>();

        //Language
        configuration.CreateMap<ApplicationLanguage, ApplicationLanguageEditDto>();
        configuration.CreateMap<ApplicationLanguage, ApplicationLanguageListDto>();
        configuration.CreateMap<NotificationDefinition, NotificationSubscriptionWithDisplayNameDto>();
        configuration.CreateMap<ApplicationLanguage, ApplicationLanguageEditDto>()
            .ForMember(ldto => ldto.IsEnabled, options => options.MapFrom(l => !l.IsDisabled));

        //Tenant
        configuration.CreateMap<Tenant, RecentTenant>();
        configuration.CreateMap<Tenant, TenantLoginInfoDto>();
        configuration.CreateMap<Tenant, TenantListDto>();
        configuration.CreateMap<TenantEditDto, Tenant>().ReverseMap();
        configuration.CreateMap<CurrentTenantInfoDto, Tenant>().ReverseMap();

        //User
        configuration.CreateMap<User, UserEditDto>()
            .ForMember(dto => dto.Password, options => options.Ignore())
            .ReverseMap()
            .ForMember(user => user.Password, options => options.Ignore());
        configuration.CreateMap<User, UserLoginInfoDto>();
        configuration.CreateMap<User, UserListDto>();
        configuration.CreateMap<User, ChatUserDto>();
        configuration.CreateMap<User, OrganizationUnitUserListDto>();
        configuration.CreateMap<Role, OrganizationUnitRoleListDto>();
        configuration.CreateMap<CurrentUserProfileEditDto, User>().ReverseMap();
        configuration.CreateMap<UserLoginAttemptDto, UserLoginAttempt>().ReverseMap();
        configuration.CreateMap<ImportUserDto, User>().ForMember(x => x.Roles, options => options.Ignore());
        configuration.CreateMap<User, FindUsersOutputDto>();
        configuration.CreateMap<User, FindOrganizationUnitUsersOutputDto>();

        //AuditLog
        configuration.CreateMap<AuditLog, AuditLogListDto>();

        //EntityChanges
        configuration.CreateMap<EntityChange, EntityChangeListDto>();
        configuration.CreateMap<EntityChange, EntityAndPropertyChangeListDto>();
        configuration.CreateMap<EntityPropertyChange, EntityPropertyChangeDto>();
        configuration.CreateMap<EntityChangePropertyAndUser, EntityChangeListDto>();

        //Friendship
        configuration.CreateMap<Friendship, FriendDto>();
        configuration.CreateMap<FriendCacheItem, FriendDto>();

        //OrganizationUnit
        configuration.CreateMap<OrganizationUnit, OrganizationUnitDto>();

        //Webhooks
        configuration.CreateMap<WebhookSubscription, GetAllSubscriptionsOutput>();
        configuration.CreateMap<WebhookSendAttempt, GetAllSendAttemptsOutput>()
            .ForMember(webhookSendAttemptListDto => webhookSendAttemptListDto.WebhookName,
                options => options.MapFrom(l => l.WebhookEvent.WebhookName))
            .ForMember(webhookSendAttemptListDto => webhookSendAttemptListDto.Data,
                options => options.MapFrom(l => l.WebhookEvent.Data));

        configuration.CreateMap<WebhookSendAttempt, GetAllSendAttemptsOfWebhookEventOutput>();

        configuration.CreateMap<DynamicProperty, DynamicPropertyDto>().ReverseMap();
        configuration.CreateMap<DynamicPropertyValue, DynamicPropertyValueDto>().ReverseMap();
        configuration.CreateMap<DynamicEntityProperty, DynamicEntityPropertyDto>()
            .ForMember(dto => dto.DynamicPropertyName,
                options => options.MapFrom(entity =>
                    entity.DynamicProperty.DisplayName.IsNullOrEmpty()
                        ? entity.DynamicProperty.PropertyName
                        : entity.DynamicProperty.DisplayName));
        configuration.CreateMap<DynamicEntityPropertyDto, DynamicEntityProperty>();

        configuration.CreateMap<DynamicEntityPropertyValue, DynamicEntityPropertyValueDto>().ReverseMap();

        //User Delegations
        configuration.CreateMap<CreateUserDelegationDto, UserDelegation>();

        /* ADD YOUR OWN CUSTOM AUTOMAPPER MAPPINGS HERE */
    }
}
