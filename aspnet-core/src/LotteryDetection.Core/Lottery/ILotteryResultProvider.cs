using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Newtonsoft.Json;

namespace LotteryDetection.Lottery;

[Table("LotteryDrawResults")]
public class LotteryDrawResult : Entity<Guid>, IHasCreationTime
{
    [Required]
    [MaxLength(256)]
    public string Province { get; set; }
    
    public DateTime DrawDate { get; set; }
    
    public string RawPrizesJson { get; set; }

    [NotMapped]
    public Dictionary<string, List<string>> Prizes
    {
        get => string.IsNullOrEmpty(RawPrizesJson) ? new Dictionary<string, List<string>>() : JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(RawPrizesJson);
        set => RawPrizesJson = JsonConvert.SerializeObject(value);
    }

    public DateTime CreationTime { get; set; }
}

public interface ILotteryResultProvider
{
    /// <summary>
    /// Gets the lottery draw result for a specific province and date.
    /// </summary>
    Task<LotteryDrawResult> GetResultAsync(string province, DateTime drawDate, bool allowScrape = true);
}

