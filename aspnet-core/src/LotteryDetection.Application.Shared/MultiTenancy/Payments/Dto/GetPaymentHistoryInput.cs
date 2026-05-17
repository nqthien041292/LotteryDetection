using Abp.Runtime.Validation;
using LotteryDetection.Common;
using LotteryDetection.Dto;

namespace LotteryDetection.MultiTenancy.Payments.Dto;

public class GetPaymentHistoryInput : PagedAndSortedInputDto, IShouldNormalize
{
    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "CreationTime";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {
            return s.Replace("editionDisplayName", "Edition.DisplayName");
        });
    }
}

