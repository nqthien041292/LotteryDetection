using System.Collections.Generic;
using System.Linq;
using Abp.Localization;
using LotteryDetection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.Migrations.Seed.Host;

public class DefaultLanguagesCreator
{
    private readonly LotteryDetectionDbContext _context;

    public DefaultLanguagesCreator(LotteryDetectionDbContext context)
    {
        _context = context;
    }

    public static List<ApplicationLanguage> InitialLanguages => GetInitialLanguages();

    private static List<ApplicationLanguage> GetInitialLanguages()
    {
        var tenantId = LotteryDetectionConsts.MultiTenancyEnabled ? null : (int?)1;
        return new List<ApplicationLanguage>
        {
            new(tenantId, "en", "English", "famfamfam-flags us"),
            new(tenantId, "en-GB", "English (UK)", "famfamfam-flags gb"),
            new(tenantId, "ar", "العربية", "famfamfam-flags sa"),
            new(tenantId, "de", "Deutsch", "famfamfam-flags de"),
            new(tenantId, "it", "Italiano", "famfamfam-flags it"),
            new(tenantId, "fr", "Français", "famfamfam-flags fr"),
            new(tenantId, "pt-BR", "Português (Brasil)", "famfamfam-flags br"),
            new(tenantId, "tr", "Türkçe", "famfamfam-flags tr"),
            new(tenantId, "ru", "Pусский", "famfamfam-flags ru"),
            new(tenantId, "zh-Hans", "简体中文", "famfamfam-flags cn"),
            new(tenantId, "es-MX", "Español (México)", "famfamfam-flags mx"),
            new(tenantId, "es", "Español (Spanish)", "famfamfam-flags es"),
            new(tenantId, "vi", "Tiếng Việt", "famfamfam-flags vn"),
            new(tenantId, "nl", "Dutch (Nederlands)", "famfamfam-flags nl"),
            new(tenantId, "th", "ภาษาไทย", "famfamfam-flags th")
        };
    }

    public void Create()
    {
        CreateLanguages();
    }

    private void CreateLanguages()
    {
        foreach (var language in InitialLanguages) AddLanguageIfNotExists(language);
    }

    private void AddLanguageIfNotExists(ApplicationLanguage language)
    {
        if (_context.Languages.IgnoreQueryFilters()
            .Any(l => l.TenantId == language.TenantId && l.Name == language.Name)) return;

        _context.Languages.Add(language);

        _context.SaveChanges();
    }
}