using Abp.Application.Services.Dto;

namespace LotteryDetection.Dto;

public class PagedAndSortedInputDto : PagedInputDto, ISortedResultRequest
{
    public string Sorting { get; set; }

    public PagedAndSortedInputDto()
    {
        MaxResultCount = AppConsts.DefaultPageSize;
    }
}

