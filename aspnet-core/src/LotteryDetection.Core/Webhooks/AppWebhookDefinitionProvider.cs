using Abp.Localization;
using Abp.Webhooks;
using LotteryDetection.Webhooks;

namespace LotteryDetection.WebHooks;

public class AppWebhookDefinitionProvider : WebhookDefinitionProvider
{
    public override void SetWebhooks(IWebhookDefinitionContext context)
    {
        context.Manager.Add(new WebhookDefinition(
            AppWebHookNames.TestWebhook
        ));

        //Add your webhook definitions here 
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, LotteryDetectionConsts.LocalizationSourceName);
    }
}