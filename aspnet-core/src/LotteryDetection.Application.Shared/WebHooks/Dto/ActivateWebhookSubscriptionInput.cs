using System;

namespace LotteryDetection.WebHooks.Dto;

public class ActivateWebhookSubscriptionInput
{
    public Guid SubscriptionId { get; set; }

    public bool IsActive { get; set; }
}

