using Abp.Application.Services;
using LotteryDetection.Tenants.Dashboard.Dto;

namespace LotteryDetection.Tenants.Dashboard;

public interface ITenantDashboardAppService : IApplicationService
{
    GetMemberActivityOutput GetMemberActivity();

    GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input);

    GetDailySalesOutput GetDailySales();

    GetProfitShareOutput GetProfitShare();

    GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input);

    GetTopStatsOutput GetTopStats();

    GetRegionalStatsOutput GetRegionalStats();

    GetGeneralStatsOutput GetGeneralStats();
}