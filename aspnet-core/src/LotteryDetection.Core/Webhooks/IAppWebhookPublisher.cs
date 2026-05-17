using System.Threading.Tasks;
using LotteryDetection.Authorization.Users;

namespace LotteryDetection.WebHooks;

public interface IAppWebhookPublisher
{
    Task PublishTestWebhook();
}

