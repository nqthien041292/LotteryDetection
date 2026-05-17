using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.Authorization.Permissions.Dto;

namespace LotteryDetection.Authorization.Permissions;

public interface IPermissionAppService : IApplicationService
{
    ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions();
}