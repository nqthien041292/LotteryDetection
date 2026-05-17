using LotteryDetection.Dto;

namespace LotteryDetection.Organizations.Dto;

public class FindOrganizationUnitUsersInput : PagedAndFilteredInputDto
{
    public long OrganizationUnitId { get; set; }
}