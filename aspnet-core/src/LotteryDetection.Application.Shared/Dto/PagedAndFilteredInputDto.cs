using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace LotteryDetection.Dto;

public class PagedAndFilteredInputDto : IPagedResultRequest
{
    public PagedAndFilteredInputDto()
    {
        MaxResultCount = AppConsts.DefaultPageSize;
    }

    public string Filter { get; set; }

    [Range(1, AppConsts.MaxPageSize)] public int MaxResultCount { get; set; }

    [Range(0, int.MaxValue)] public int SkipCount { get; set; }
}