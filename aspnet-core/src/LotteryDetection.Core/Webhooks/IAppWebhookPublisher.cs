using System.Threading.Tasks;

namespace LotteryDetection.WebHooks;

public interface IAppWebhookPublisher
{
    Task PublishTestWebhook();
}