using System.Threading.Tasks;
using LotteryDetection.Sessions.Dto;

namespace LotteryDetection.Web.Session;

public interface IPerRequestSessionCache
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync();
}

