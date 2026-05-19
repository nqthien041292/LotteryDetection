using System.Threading;
using System.Threading.Tasks;

namespace LotteryDetection.Lottery.Gcp;

public interface IVertexAITicketAnalyzer
{
    Task<VertexAIAnalysisResult> AnalyzeAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default);
}
