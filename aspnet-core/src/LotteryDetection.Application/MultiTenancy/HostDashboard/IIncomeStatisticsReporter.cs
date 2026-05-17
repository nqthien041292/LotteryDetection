using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LotteryDetection.MultiTenancy.HostDashboard.Dto;

namespace LotteryDetection.MultiTenancy.HostDashboard;

public interface IIncomeStatisticsService
{
    Task<List<IncomeStastistic>> GetIncomeStatisticsData(DateTime startDate, DateTime endDate,
        ChartDateInterval dateInterval);
}