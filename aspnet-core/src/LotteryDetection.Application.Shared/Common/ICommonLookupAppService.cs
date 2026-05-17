using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.Common.Dto;
using LotteryDetection.Editions.Dto;

namespace LotteryDetection.Common;

public interface ICommonLookupAppService : IApplicationService
{
    Task<ListResultDto<SubscribableEditionComboboxItemDto>> GetEditionsForCombobox(bool onlyFreeItems = false);

    Task<PagedResultDto<FindUsersOutputDto>> FindUsers(FindUsersInput input);

    GetDefaultEditionNameOutput GetDefaultEditionName();
}

