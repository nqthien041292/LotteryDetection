using System;

namespace LotteryDetection.Net.Emailing;

[Serializable]
public class EmailTemplateCacheItem
{
    public const string CacheName = "AppEmailTemplateCache";

    public EmailTemplateCacheItem(string template)
    {
        Template = template;
    }

    public string Template { get; private set; }
}