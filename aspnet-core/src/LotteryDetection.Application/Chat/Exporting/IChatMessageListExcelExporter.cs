using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using LotteryDetection.Chat.Dto;
using LotteryDetection.Dto;

namespace LotteryDetection.Chat.Exporting
{
    public interface IChatMessageListExcelExporter
    {
        Task<FileDto> ExportToFile(UserIdentifier user, List<ChatMessageExportDto> messages);
    }
}