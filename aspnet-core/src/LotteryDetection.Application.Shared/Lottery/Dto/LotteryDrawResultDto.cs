using System;
using System.Collections.Generic;

namespace LotteryDetection.Lottery.Dto;

public class LotteryDrawResultDto
{
    public string Province { get; set; }
    public DateTime DrawDate { get; set; }
    public Dictionary<string, List<string>> Prizes { get; set; }
}
