using System.Threading.Tasks;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IPushNotificationService
{
    Task InitializeAsync();
    Task<string> GetTokenAsync();
    Task RegisterTokenAsync();
}
