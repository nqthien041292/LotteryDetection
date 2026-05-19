using LotteryDetection.Mobile.Models.Lottery;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface ILotteryDetectionService
{
    Task<LotteryTicketResult> AnalyzeAsync(string imagePath, CancellationToken ct);
}
