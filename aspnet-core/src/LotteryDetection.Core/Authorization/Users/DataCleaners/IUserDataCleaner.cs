using Abp;
using System.Threading.Tasks;

namespace LotteryDetection.Authorization.Users.DataCleaners;

public interface IUserDataCleaner
{
    Task CleanUserData(UserIdentifier userIdentifier);
}

