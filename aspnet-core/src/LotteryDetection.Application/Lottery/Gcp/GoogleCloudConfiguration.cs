using Abp.Dependency;
using LotteryDetection.Configuration;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Lottery.Gcp;

public class GoogleCloudConfiguration : ITransientDependency
{
    private readonly IConfigurationRoot _configuration;

    public GoogleCloudConfiguration(IAppConfigurationAccessor accessor)
    {
        _configuration = accessor.Configuration;
    }

    public string ProjectId => _configuration["GoogleCloud:ProjectId"];

    public string Location => _configuration["GoogleCloud:Location"] ?? "us-central1";

    public string VertexAIModel => _configuration["GoogleCloud:VertexAI:Model"] ?? "gemini-1.5-pro-002";

    public int VertexAIMaxOutputTokens =>
        int.TryParse(_configuration["GoogleCloud:VertexAI:MaxOutputTokens"], out var n) ? n : 2048;

    public double VertexAITemperature =>
        double.TryParse(_configuration["GoogleCloud:VertexAI:Temperature"], out var t) ? t : 0.1;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectId);
}
