using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HtmlAgilityPack;
using LotteryDetection.Lottery;
using Microsoft.EntityFrameworkCore;
using Abp.UI;

namespace LotteryDetection.Lottery.Scraping;

public class MinhNgocResultProvider : ILotteryResultProvider, ITransientDependency
{
    private readonly IRepository<LotteryDrawResult, Guid> _repository;
    private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    public MinhNgocResultProvider(IRepository<LotteryDrawResult, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<LotteryDrawResult> GetResultAsync(string province, DateTime drawDate)
    {
        var cached = await _repository.GetAll()
            .FirstOrDefaultAsync(x => x.Province == province && x.DrawDate.Date == drawDate.Date);

        if (cached != null)
        {
            return cached;
        }

        var scraped = await ScrapeFromMinhNgocAsync(province, drawDate);
        if (scraped == null)
        {
            throw new UserFriendlyException($"Không thể lấy kết quả xổ số đài {province} ngày {drawDate:dd/MM/yyyy}.");
        }

        await _repository.InsertAsync(scraped);
        return scraped;
    }

    private async Task<LotteryDrawResult> ScrapeFromMinhNgocAsync(string province, DateTime drawDate)
    {
        // Example URL: https://www.minhngoc.net.vn/ket-qua-xo-so/mien-nam/tp-hcm/18-05-2026.html
        var regionStr = GetRegionFromProvince(province);
        if (string.IsNullOrEmpty(regionStr)) return null;

        var provSlug = GetProvinceSlug(province);
        var dateStr = drawDate.ToString("dd-MM-yyyy");

        var url = $"https://www.minhngoc.net.vn/ket-qua-xo-so/{regionStr}/{provSlug}/{dateStr}.html";
        
        try
        {
            var html = await HttpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new LotteryDrawResult
            {
                Province = province,
                DrawDate = drawDate,
                Prizes = new Dictionary<string, List<string>>()
            };

            // Mapping class names from minhngoc HTML to prize names
            var classMap = new Dictionary<string, string>
            {
                {"giaidb", "Special"},
                {"giai1", "First"},
                {"giai2", "Second"},
                {"giai3", "Third"},
                {"giai4", "Fourth"},
                {"giai5", "Fifth"},
                {"giai6", "Sixth"},
                {"giai7", "Seventh"},
                {"giai8", "Eighth"}
            };

            foreach (var kvp in classMap)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//td[contains(@class, '{kvp.Key}')]");
                if (nodes != null)
                {
                    var numbers = new List<string>();
                    foreach (var node in nodes)
                    {
                        var text = node.InnerText.Trim();
                        // numbers can be separated by spaces or inside divs
                        var parts = text.Split(new[] { ' ', '\n', '\r', '-' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (part.All(char.IsDigit))
                            {
                                numbers.Add(part);
                            }
                        }
                    }
                    if (numbers.Any())
                    {
                        result.Prizes[kvp.Value] = numbers.Distinct().ToList();
                    }
                }
            }

            if (!result.Prizes.Any())
            {
                return null;
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private string GetRegionFromProvince(string province)
    {
        var p = province.ToLower();
        if (p.Contains("tp. hcm") || p.Contains("hồ chí minh") || p.Contains("an giang") || p.Contains("bạc liêu") || p.Contains("bến tre")) return "mien-nam";
        return "mien-nam"; // simplified for demo
    }

    private string GetProvinceSlug(string province)
    {
        var p = province.ToLower();
        if (p.Contains("tp. hcm") || p.Contains("hồ chí minh")) return "tp-hcm";
        if (p.Contains("bạc liêu")) return "bac-lieu";
        if (p.Contains("bến tre")) return "ben-tre";
        // simplified mapping
        return "tp-hcm";
    }
}
