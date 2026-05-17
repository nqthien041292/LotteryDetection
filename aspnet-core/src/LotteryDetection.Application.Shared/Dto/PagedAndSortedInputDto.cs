using Abp.Application.Services.Dto;

namespace LotteryDetection.Dto;

public class PagedAndSortedInputDto : PagedInputDto, ISortedResultRequest
{
    public PagedAndSortedInputDto()
    {
        MaxResultCount = AppConsts.DefaultPageSize;
    }

    public string Sorting { get; set; }
}