using System.Collections.Generic;

namespace LotteryDetection.DynamicEntityProperties;

public interface IDynamicEntityPropertyDefinitionAppService
{
    List<string> GetAllAllowedInputTypeNames();

    List<string> GetAllEntities();
}