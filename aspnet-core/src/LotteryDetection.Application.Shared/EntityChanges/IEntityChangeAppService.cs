using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.EntityChanges.Dto;

namespace LotteryDetection.EntityChanges;

public interface IEntityChangeAppService : IApplicationService
{
    Task<ListResultDto<EntityAndPropertyChangeListDto>> GetEntityChangesByEntity(GetEntityChangesByEntityInput input);
}