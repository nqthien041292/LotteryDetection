using Abp.Domain.Services;

namespace LotteryDetection.Authorization.Users.Password;

public interface IPasswordExpirationService : IDomainService
{
    void ForcePasswordExpiredUsersToChangeTheirPassword();
}