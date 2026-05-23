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
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly IRepository<LotteryDrawResult, Guid> _repository;


    public MinhNgocResultProvider(IRepository<LotteryDrawResult, Guid> repository)
    {
        _repository = repository;
    }

    private bool IsDrawTimeReached(string province, DateTime drawDate)
    {
        var vnTime = DateTime.UtcNow.AddHours(7);
        if (drawDate.Date > vnTime.Date)
        {
            return false; // Ngày tương lai
        }

        if (drawDate.Date == vnTime.Date)
        {
            var regionStr = GetRegionFromProvince(province);
            var nowTime = vnTime.TimeOfDay;

            if (regionStr == "mien-bac")
            {
                return nowTime >= new TimeSpan(18, 15, 0); // Miền Bắc quay lúc 18h15
            }
            if (regionStr == "mien-trung")
            {
                return nowTime >= new TimeSpan(17, 15, 0); // Miền Trung quay lúc 17h15
            }
            return nowTime >= new TimeSpan(16, 15, 0); // Miền Nam quay lúc 16h15
        }

        return true; // Quá khứ
    }

    public async Task<LotteryDrawResult> GetResultAsync(string province, DateTime drawDate)
    {
        var cached = await _repository.GetAll()
            .FirstOrDefaultAsync(x => x.Province == province && x.DrawDate.Date == drawDate.Date);

        if (cached != null)
        {
            return cached;
        }

        if (!IsDrawTimeReached(province, drawDate))
        {
            return null; // Chưa đến giờ quay thưởng
        }

        var scraped = await ScrapeFromMinhNgocAsync(province, drawDate);
        if (scraped == null)
        {
            return null; // Chưa có kết quả chính thức từ nhà đài
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
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Headers.Referrer = new Uri("https://www.minhngoc.net.vn/");

            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Minh Ngoc returned status code {response.StatusCode}");
            }

            var html = await response.Content.ReadAsStringAsync();

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
                if (nodes != null && nodes.Count > 1)
                {
                    var node = nodes[1]; // nodes[0] is the header (e.g. 'Giải ĐB'), nodes[1] is the first result column
                    var numbers = new List<string>();
                    
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
        catch (Exception)
        {
            return null;
        }
    }

    private LotteryDrawResult GetMockResult(string province, DateTime drawDate)
    {
        return new LotteryDrawResult
        {
            Province = province,
            DrawDate = drawDate.Date,
            Prizes = new Dictionary<string, List<string>>
            {
                { "Special", new List<string> { "898665" } },
                { "First", new List<string> { "12345" } },
                { "Second", new List<string> { "23456" } },
                { "Third", new List<string> { "34567", "45678" } },
                { "Fourth", new List<string> { "11111", "22222", "33333", "44444", "55555", "66666", "77777" } },
                { "Fifth", new List<string> { "8888" } },
                { "Sixth", new List<string> { "9999", "0000", "1111" } },
                { "Seventh", new List<string> { "222" } },
                { "Eighth", new List<string> { "33" } }
            }
        };
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
        if (p.Contains("an giang")) return "an-giang";
        if (p.Contains("vũng tàu")) return "vung-tau";
        if (p.Contains("cần thơ")) return "can-tho";
        if (p.Contains("đồng nai")) return "dong-nai";
        if (p.Contains("sóc trăng")) return "soc-trang";
        if (p.Contains("tây ninh")) return "tay-ninh";
        if (p.Contains("bình thuận")) return "binh-thuan";
        if (p.Contains("vĩnh long")) return "vinh-long";
        if (p.Contains("bình dương")) return "binh-duong";
        if (p.Contains("trà vinh")) return "tra-vinh";
        if (p.Contains("long an")) return "long-an";
        if (p.Contains("bình phước")) return "binh-phuoc";
        if (p.Contains("hậu giang")) return "hau-giang";
        if (p.Contains("tiền giang")) return "tien-giang";
        if (p.Contains("kiên giang")) return "kien-giang";
        if (p.Contains("đồng tháp")) return "dong-thap";
        if (p.Contains("cà mau")) return "ca-mau";
        if (p.Contains("đà lạt")) return "da-lat";
        
        // Default fallback if parsing fails or province is unknown
        return "tp-hcm";
    }
}
