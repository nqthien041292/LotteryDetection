using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Configuration;

public interface IAppConfigurationAccessor
{
    IConfigurationRoot Configuration { get; }
}

