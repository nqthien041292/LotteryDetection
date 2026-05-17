using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LotteryDetection.EntityChanges.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LotteryDetection.EntityChanges;

public interface IEntityChangeAppService : IApplicationService
{
    Task<ListResultDto<EntityAndPropertyChangeListDto>> GetEntityChangesByEntity(GetEntityChangesByEntityInput input);
}

