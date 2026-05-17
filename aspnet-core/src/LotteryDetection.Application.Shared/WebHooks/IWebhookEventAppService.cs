using System.Threading.Tasks;
using Abp.Webhooks;

namespace LotteryDetection.WebHooks;

public interface IWebhookEventAppService
{
    Task<WebhookEvent> Get(string id);
}