using System.Threading.Tasks;
using Abp;
using Abp.Domain.Services;
using LotteryDetection.Authorization.Users;

namespace LotteryDetection.Authorization.QrLogin;

public interface IQrLoginManager : IDomainService
{
    Task<string> GenerateSessionId(string connectionId);

    Task<bool> VerifySessionId(string connectionId, string sessionId);

    Task SendAuthData(string connectionId, QrLoginAuthenticateResultModel model);

    Task RemoveQrLoginCache(string connectionId);

    Task<User> GetUserByUserIdentifierClaimAsync(UserIdentifier userIdentifier);
}