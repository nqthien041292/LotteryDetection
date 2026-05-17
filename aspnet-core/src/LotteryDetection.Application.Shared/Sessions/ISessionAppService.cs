using System.Threading.Tasks;
using Abp.Application.Services;
using LotteryDetection.Sessions.Dto;

namespace LotteryDetection.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();

    Task<UpdateUserSignInTokenOutput> UpdateUserSignInToken();
}