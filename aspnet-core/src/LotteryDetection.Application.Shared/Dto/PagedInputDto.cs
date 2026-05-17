using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace LotteryDetection.Dto;

public class PagedInputDto : IPagedResultRequest
{
    public PagedInputDto()
    {
        MaxResultCount = AppConsts.DefaultPageSize;
    }

    [Range(1, AppConsts.MaxPageSize)] public int MaxResultCount { get; set; }

    [Range(0, int.MaxValue)] public int SkipCount { get; set; }
}