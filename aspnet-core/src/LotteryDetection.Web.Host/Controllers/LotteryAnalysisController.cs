using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Application.Services.Dto;
using Abp.IO.Extensions;
using Abp.UI;
using LotteryDetection.Authorization;
using LotteryDetection.Lottery;
using LotteryDetection.Lottery.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LotteryDetection.Web.Controllers;

[Route("api/services/app/[controller]/[action]")]
[AbpMvcAuthorize]
public class LotteryAnalysisController : LotteryDetectionControllerBase
{
    private readonly ITicketAnalysisAppService _appService;

    public LotteryAnalysisController(ITicketAnalysisAppService appService)
    {
        _appService = appService;
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<TicketAnalysisDto> AnalyzeTicket()
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            throw new UserFriendlyException("Vui lòng upload ảnh.");

        byte[] bytes;
        await using (var stream = file.OpenReadStream())
        {
            bytes = stream.GetAllBytes();
        }

        return await _appService.AnalyzeAsync(new AnalyzeTicketInput
        {
            ImageBytes = bytes,
            ContentType = file.ContentType,
            FileName = Path.GetFileName(file.FileName)
        });
    }

    [HttpGet]
    [AbpMvcAuthorize]
    public Task<PagedResultDto<TicketAnalysisDto>> GetHistory(int maxResultCount = 50, int skipCount = 0)
    {
        return _appService.GetHistoryAsync(new PagedResultRequestDto
        {
            MaxResultCount = Math.Clamp(maxResultCount, 1, 200),
            SkipCount = Math.Max(0, skipCount)
        });
    }

    [HttpPost]
    [AbpMvcAuthorize]
    public Task Delete([FromBody] EntityDto<Guid> input)
    {
        return _appService.DeleteAsync(input);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [CloudSchedulerAuthorize]
    public async Task<IActionResult> CheckPendingResults()
    {
        await _appService.CheckPendingResultsAsync();
        return Ok(new { success = true, message = "Pending results checked successfully." });
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> TriggerScrapeLast7Days()
    {
        await _appService.TriggerScrapeLast7DaysAsync();
        return Ok(new { success = true, message = "Manual trigger scrape for last 7 days completed." });
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> TriggerScrapeLast30Days()
    {
        await _appService.TriggerScrapeLast30DaysAsync();
        return Ok(new { success = true, message = "Manual trigger scrape for last 30 days completed." });
    }
}

