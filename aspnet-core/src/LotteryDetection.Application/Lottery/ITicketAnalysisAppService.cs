using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.Lottery.Dto;

namespace LotteryDetection.Lottery;

public interface ITicketAnalysisAppService : IApplicationService
{
    Task<System.Collections.Generic.List<TicketAnalysisDto>> AnalyzeAsync(AnalyzeTicketInput input);

    Task<TicketAnalysisDto> GetAsync(EntityDto<Guid> input);

    Task<PagedResultDto<TicketAnalysisDto>> GetHistoryAsync(PagedResultRequestDto input);

    Task DeleteAsync(EntityDto<Guid> input);

    Task CheckPendingResultsAsync();

    Task TriggerScrapeLast7DaysAsync();

    Task TriggerScrapeLast30DaysAsync();

    Task TriggerScrapeFromDateAsync(DateTime startDate);

    Task<System.Collections.Generic.List<LotteryDrawResultDto>> GetDrawResultsAsync(DateTime drawDate);

    Task<HistoryStatsDto> GetHistoryStatsAsync();
}


