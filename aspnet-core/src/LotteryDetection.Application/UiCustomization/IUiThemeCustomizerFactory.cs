using System.Threading.Tasks;
using Abp.Dependency;

namespace LotteryDetection.UiCustomization;

public interface IUiThemeCustomizerFactory : ISingletonDependency
{
    Task<IUiCustomizer> GetCurrentUiCustomizer();

    IUiCustomizer GetUiCustomizer(string theme);
}
