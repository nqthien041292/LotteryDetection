using System.Threading.Tasks;
using Abp;
using Abp.Domain.Services;

namespace LotteryDetection.Authorization.Users.Profile;

public interface IProfileImageService : IDomainService
{
    Task<string> GetProfilePictureContentForUser(UserIdentifier userIdentifier);
}