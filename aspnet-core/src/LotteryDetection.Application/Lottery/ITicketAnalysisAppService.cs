using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.Lottery.Dto;

namespace LotteryDetection.Lottery;

public interface ITicketAnalysisAppService : IApplicationService
{
    Task<TicketAnalysisDto> AnalyzeAsync(AnalyzeTicketInput input);

    Task<TicketAnalysisDto> GetAsync(EntityDto<Guid> input);

    Task<PagedResultDto<TicketAnalysisDto>> GetHistoryAsync(PagedResultRequestDto input);

    Task CheckPendingResultsAsync();
}
