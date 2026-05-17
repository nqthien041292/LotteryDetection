using System.Collections.Generic;
using Abp.Dependency;

namespace LotteryDetection.DashboardCustomization.Definitions.Cache;

public interface IWidgetFilterDefinitionCacheManager : ITransientDependency
{
    List<WidgetFilterDefinition> GetAll();

    void Set(List<WidgetFilterDefinition> definition);
}

