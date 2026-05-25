using System.Threading;
using System.Threading.Tasks;

namespace LotteryDetection.Lottery.Gcp;

public interface IVertexAITicketAnalyzer
{
    Task<System.Collections.Generic.List<VertexAIAnalysisResult>> AnalyzeAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default);
}
