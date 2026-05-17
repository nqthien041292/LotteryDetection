using System.Collections.Generic;

namespace LotteryDetection.Logging.Dto;

public class GetLatestWebLogsOutput
{
    public List<string> LatestWebLogLines { get; set; }
}