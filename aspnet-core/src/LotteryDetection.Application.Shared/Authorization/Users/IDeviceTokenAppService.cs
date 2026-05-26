using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.Authorization.Users.Dto;

namespace LotteryDetection.Authorization.Users;

public interface IDeviceTokenAppService : IApplicationService
{
    Task RegisterDeviceToken(RegisterDeviceTokenInput input);
}
