using LotteryDetection.Dto;

namespace LotteryDetection.WebHooks.Dto;

public class GetAllSendAttemptsInput : PagedInputDto
{
    public string SubscriptionId { get; set; }
}

