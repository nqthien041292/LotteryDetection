using System.Threading.Tasks;

namespace LotteryDetection.Security;

public interface IPasswordComplexitySettingStore
{
    Task<PasswordComplexitySetting> GetSettingsAsync();
}