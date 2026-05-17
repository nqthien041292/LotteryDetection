using LotteryDetection.Dto;

namespace LotteryDetection.Organizations.Dto;

public class FindOrganizationUnitRolesInput : PagedAndFilteredInputDto
{
    public long OrganizationUnitId { get; set; }
}