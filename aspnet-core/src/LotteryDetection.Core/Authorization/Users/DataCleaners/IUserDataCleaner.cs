using System.Threading.Tasks;
using Abp;

namespace LotteryDetection.Authorization.Users.DataCleaners;

public interface IUserDataCleaner
{
    Task CleanUserData(UserIdentifier userIdentifier);
}