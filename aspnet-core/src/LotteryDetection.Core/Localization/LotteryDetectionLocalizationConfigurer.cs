using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace LotteryDetection.Localization;

public static class LotteryDetectionLocalizationConfigurer
{
    public static void Configure(ILocalizationConfiguration localizationConfiguration)
    {
        localizationConfiguration.Sources.Add(
            new DictionaryBasedLocalizationSource(
                LotteryDetectionConsts.LocalizationSourceName,
                new XmlEmbeddedFileLocalizationDictionaryProvider(
                    typeof(LotteryDetectionLocalizationConfigurer).GetAssembly(),
                    "LotteryDetection.Localization.LotteryDetection"
                )
            )
        );
    }
}