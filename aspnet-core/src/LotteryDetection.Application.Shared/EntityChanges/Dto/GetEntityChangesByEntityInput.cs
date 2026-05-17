using LotteryDetection.Dto;
using System;

namespace LotteryDetection.EntityChanges.Dto;

public class GetEntityChangesByEntityInput
{
    public string EntityTypeFullName { get; set; }
    public string EntityId { get; set; }
}

