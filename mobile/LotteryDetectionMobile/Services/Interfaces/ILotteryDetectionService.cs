using LotteryDetectionMobile.Models.Lottery;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface ILotteryDetectionService
{
    Task<LotteryTicketResult> AnalyzeAsync(string imagePath, CancellationToken ct);
}
