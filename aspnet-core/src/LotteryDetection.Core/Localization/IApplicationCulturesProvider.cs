using System.Globalization;

namespace LotteryDetection.Localization;

public interface IApplicationCulturesProvider
{
    CultureInfo[] GetAllCultures();
}