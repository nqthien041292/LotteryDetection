using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.Authorization.Users.Dto;

namespace LotteryDetection.Authorization.Users;

public interface IUserLoginAppService : IApplicationService
{
    Task<PagedResultDto<UserLoginAttemptDto>> GetUserLoginAttempts(GetLoginAttemptsInput input);
    Task<string> GetExternalLoginProviderNameByUser(long userId);
}

