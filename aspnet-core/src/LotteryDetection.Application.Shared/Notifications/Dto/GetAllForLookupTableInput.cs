using Abp.Application.Services.Dto;

namespace LotteryDetection.Notifications.Dto;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}