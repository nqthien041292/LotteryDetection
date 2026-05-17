using LotteryDetection.Dto;

namespace LotteryDetection.Common.Dto;

public class FindUsersInput : PagedAndFilteredInputDto
{
    public int? TenantId { get; set; }

    public bool ExcludeCurrentUser { get; set; }
}

