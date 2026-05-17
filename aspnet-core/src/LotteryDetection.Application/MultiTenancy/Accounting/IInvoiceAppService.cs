using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using LotteryDetection.MultiTenancy.Accounting.Dto;

namespace LotteryDetection.MultiTenancy.Accounting;

public interface IInvoiceAppService
{
    Task<InvoiceDto> GetInvoiceInfo(EntityDto<long> input);

    Task CreateInvoice(CreateInvoiceDto input);
}
