using System;
using Abp.Authorization;
using Abp.Runtime.Validation;
using LotteryDetection.Dto;

namespace LotteryDetection.Authorization.Users.Dto;

public class GetLoginAttemptsInput : PagedAndSortedInputDto, IGetLoginAttemptsInput, IShouldNormalize
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public AbpLoginResultType? Result { get; set; }
    public string Filter { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting)) Sorting = "CreationTime DESC";

        Filter = Filter?.Trim();
    }
}