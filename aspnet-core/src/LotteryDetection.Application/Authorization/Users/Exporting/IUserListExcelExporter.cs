using System.Collections.Generic;
using System.Threading.Tasks;
using LotteryDetection.Authorization.Users.Dto;
using LotteryDetection.Dto;

namespace LotteryDetection.Authorization.Users.Exporting;

public interface IUserListExcelExporter
{
    Task<FileDto> ExportToFile(List<UserListDto> userListDtos, List<string> selectedColumns);
}