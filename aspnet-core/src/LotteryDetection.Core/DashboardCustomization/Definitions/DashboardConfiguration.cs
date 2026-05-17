using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using LotteryDetection.Authorization;
using LotteryDetection.DashboardCustomization.Definitions.Cache;

namespace LotteryDetection.DashboardCustomization.Definitions;

public class DashboardConfiguration : ITransientDependency
{
    public const string HostWidgetDefinitionsCacheName = "HostWidgetDefinitionsCache";

    private readonly IAbpSession _abpSession;

    private readonly IDashboardDefinitionCacheManager _dashboardDefinitionCacheManager;
    private readonly IWidgetDefinitionCacheManager _widgetDefinitionCacheManager;
    private readonly IWidgetFilterDefinitionCacheManager _widgetFilterDefinitionCacheManager;
    public string TenantWidgetDefinitionsCacheName = "TenantWidgetDefinitionsCache";

    public DashboardConfiguration(
        IDashboardDefinitionCacheManager dashboardDefinitionCacheManager,
        IWidgetDefinitionCacheManager widgetDefinitionCacheManager,
        IWidgetFilterDefinitionCacheManager widgetFilterDefinitionCacheManager,
        IAbpSession abpSession)
    {
        _dashboardDefinitionCacheManager = dashboardDefinitionCacheManager;
        _widgetDefinitionCacheManager = widgetDefinitionCacheManager;
        _widgetFilterDefinitionCacheManager = widgetFilterDefinitionCacheManager;
        _abpSession = abpSession;

        #region FilterDefinitions

        // These are global filter which all widgets can use
        var dateRangeFilter = new WidgetFilterDefinition(
            LotteryDetectionDashboardCustomizationConsts.Filters.FilterDateRangePicker,
            "FilterDateRangePicker"
        );

        WidgetFilterDefinitions.AddRange(new List<WidgetFilterDefinition>
        {
            dateRangeFilter
            // Add your filters here
        });

        #endregion

        #region WidgetDefinitions

        // Define Widgets

        #region TenantWidgets

        var simplePermissionDependencyForTenantDashboard =
            new LotteryDetectionSimplePermissionDependency(AppPermissions.Pages_Tenant_Dashboard);

        var dailySales = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.DailySales,
            "WidgetDailySales",
            MultiTenancySides.Tenant,
            new List<string> { dateRangeFilter.Id },
            simplePermissionDependencyForTenantDashboard
        );

        var generalStats = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.GeneralStats,
            "WidgetGeneralStats",
            MultiTenancySides.Tenant,
            permissionDependency: new LotteryDetectionSimplePermissionDependency(
                true,
                AppPermissions.Pages_Tenant_Dashboard,
                AppPermissions.Pages_Administration_AuditLogs
            )
        );

        var profitShare = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.ProfitShare,
            "WidgetProfitShare",
            MultiTenancySides.Tenant,
            permissionDependency: simplePermissionDependencyForTenantDashboard
        );

        var memberActivity = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.MemberActivity,
            "WidgetMemberActivity",
            MultiTenancySides.Tenant,
            permissionDependency: simplePermissionDependencyForTenantDashboard
        );

        var regionalStats = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.RegionalStats,
            "WidgetRegionalStats",
            MultiTenancySides.Tenant,
            permissionDependency: simplePermissionDependencyForTenantDashboard
        );

        var salesSummary = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.SalesSummary,
            "WidgetSalesSummary",
            usedWidgetFilters: new List<string> { dateRangeFilter.Id },
            side: MultiTenancySides.Tenant,
            permissionDependency: simplePermissionDependencyForTenantDashboard
        );

        var topStats = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Tenant.TopStats,
            "WidgetTopStats",
            MultiTenancySides.Tenant,
            permissionDependency: simplePermissionDependencyForTenantDashboard
        );

        WidgetDefinitions.AddRange(
            new List<WidgetDefinition>
            {
                generalStats,
                dailySales,
                profitShare,
                memberActivity,
                regionalStats,
                topStats,
                salesSummary
                // Add your tenant side widgets here
            });

        #endregion

        #region HostWidgets

        var simplePermissionDependencyForHostDashboard =
            new LotteryDetectionSimplePermissionDependency(AppPermissions.Pages_Administration_Host_Dashboard);

        var incomeStatistics = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Host.IncomeStatistics,
            "WidgetIncomeStatistics",
            MultiTenancySides.Host,
            permissionDependency: simplePermissionDependencyForHostDashboard
        );

        var hostTopStats = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Host.TopStats,
            "WidgetTopStats",
            MultiTenancySides.Host,
            permissionDependency: simplePermissionDependencyForHostDashboard
        );

        var editionStatistics = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Host.EditionStatistics,
            "WidgetEditionStatistics",
            MultiTenancySides.Host,
            permissionDependency: simplePermissionDependencyForHostDashboard
        );

        var subscriptionExpiringTenants = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Host.SubscriptionExpiringTenants,
            "WidgetSubscriptionExpiringTenants",
            MultiTenancySides.Host,
            permissionDependency: simplePermissionDependencyForHostDashboard
        );

        var recentTenants = new WidgetDefinition(
            LotteryDetectionDashboardCustomizationConsts.Widgets.Host.RecentTenants,
            "WidgetRecentTenants",
            MultiTenancySides.Host,
            new List<string> { dateRangeFilter.Id },
            simplePermissionDependencyForHostDashboard
        );

        WidgetDefinitions.AddRange(new List<WidgetDefinition>
        {
            incomeStatistics,
            hostTopStats,
            editionStatistics,
            subscriptionExpiringTenants,
            recentTenants
            // Add your host side widgets here
        });

        #endregion

        #endregion

        #region DashboardDefinitions

        // Create dashboard
        var defaultTenantDashboard = new DashboardDefinition(
            LotteryDetectionDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
            new List<string>
            {
                generalStats.Id, dailySales.Id, profitShare.Id, memberActivity.Id, regionalStats.Id, topStats.Id,
                salesSummary.Id
            });

        DashboardDefinitions.Add(defaultTenantDashboard);

        var defaultHostDashboard = new DashboardDefinition(
            LotteryDetectionDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
            new List<string>
            {
                incomeStatistics.Id,
                hostTopStats.Id,
                editionStatistics.Id,
                subscriptionExpiringTenants.Id,
                recentTenants.Id
            });

        DashboardDefinitions.Add(defaultHostDashboard);

        // Add your dashboard definition here

        #endregion
    }

    private List<DashboardDefinition> DashboardDefinitions { get; } = new();
    private List<WidgetDefinition> WidgetDefinitions { get; } = new();
    private List<WidgetFilterDefinition> WidgetFilterDefinitions { get; } = new();

    public DashboardDefinition GetDashboardDefinition(string name)
    {
        var dashboardDefinition = _dashboardDefinitionCacheManager.Get(name);
        if (dashboardDefinition == null)
        {
            dashboardDefinition = DashboardDefinitions.Find(d => d.Name == name);
            _dashboardDefinitionCacheManager.Set(dashboardDefinition);
        }

        return dashboardDefinition;
    }

    public WidgetDefinition GetWidgetDefinition(string id)
    {
        var widgets = GetWidgetDefinitions();
        return widgets.Find(w => w.Id == id);
    }

    public List<WidgetDefinition> GetWidgetDefinitions()
    {
        var widgetDefinitionKey = _abpSession.MultiTenancySide == MultiTenancySides.Host
            ? HostWidgetDefinitionsCacheName
            : TenantWidgetDefinitionsCacheName;

        var widgetDefinitions = _widgetDefinitionCacheManager.GetAll(widgetDefinitionKey);
        if (widgetDefinitions == null)
        {
            widgetDefinitions = WidgetDefinitions.Where(e => e.Side == _abpSession.MultiTenancySide).ToList();
            _widgetDefinitionCacheManager.Set(widgetDefinitionKey, widgetDefinitions);
        }

        return widgetDefinitions;
    }

    public List<WidgetFilterDefinition> GetWidgetFilterDefinitions()
    {
        var filterDefinitions = _widgetFilterDefinitionCacheManager.GetAll();
        if (filterDefinitions == null)
        {
            filterDefinitions = WidgetFilterDefinitions;
            _widgetFilterDefinitionCacheManager.Set(filterDefinitions);
        }

        return filterDefinitions;
    }
}