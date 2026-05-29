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
            new(tenantId, "vi", "Tiếng Việt", "famfamfam-flags vn"),
            new(tenantId, "en", "English", "famfamfam-flags us")
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