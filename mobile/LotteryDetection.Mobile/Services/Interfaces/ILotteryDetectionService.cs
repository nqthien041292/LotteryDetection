using LotteryDetection.Mobile.Models.Lottery;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface ILotteryDetectionService
{
    Task<List<LotteryTicketResult>> AnalyzeAsync(string imagePath, CancellationToken ct);
    Task RegisterDeviceTokenAsync(string token, string deviceType, string deviceName);
}
