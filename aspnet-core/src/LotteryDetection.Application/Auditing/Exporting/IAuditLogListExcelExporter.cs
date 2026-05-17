using System.Collections.Generic;
using System.Threading.Tasks;
using LotteryDetection.Auditing.Dto;
using LotteryDetection.Dto;
using LotteryDetection.EntityChanges.Dto;

namespace LotteryDetection.Auditing.Exporting;

public interface IAuditLogListExcelExporter
{
    Task<FileDto> ExportToFile(List<AuditLogListDto> auditLogListDtos);

    Task<FileDto> ExportToFile(List<EntityChangeListDto> entityChangeListDtos);
}