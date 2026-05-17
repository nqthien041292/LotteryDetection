using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace LotteryDetection.Localization.Dto;

public class GetLanguagesOutput : ListResultDto<ApplicationLanguageListDto>
{
    public GetLanguagesOutput()
    {
    }

    public GetLanguagesOutput(IReadOnlyList<ApplicationLanguageListDto> items, string defaultLanguageName)
        : base(items)
    {
        DefaultLanguageName = defaultLanguageName;
    }

    public string DefaultLanguageName { get; set; }
}