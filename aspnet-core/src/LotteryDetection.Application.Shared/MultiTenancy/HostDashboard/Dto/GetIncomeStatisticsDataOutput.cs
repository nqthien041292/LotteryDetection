using System.Collections.Generic;

namespace LotteryDetection.MultiTenancy.HostDashboard.Dto;

public class GetIncomeStatisticsDataOutput
{
    public GetIncomeStatisticsDataOutput(List<IncomeStastistic> incomeStatistics)
    {
        IncomeStatistics = incomeStatistics;
    }

    public List<IncomeStastistic> IncomeStatistics { get; set; }
}