using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HtmlAgilityPack;
using LotteryDetection.Lottery;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PuppeteerSharp;

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

    public Castle.Core.Logging.ILogger Logger { get; set; } = Castle.Core.Logging.NullLogger.Instance;


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

    public async Task<LotteryDrawResult> GetResultAsync(string province, DateTime drawDate, bool allowScrape = true)
    {
        var cached = await _repository.GetAll()
            .FirstOrDefaultAsync(x => x.Province == province && x.DrawDate.Date == drawDate.Date);

        if (cached != null && !allowScrape)
        {
            return cached;
        }

        if (!allowScrape)
        {
            return null; // Không tự động cào khi user submit vé số trực tiếp
        }

        if (!IsDrawTimeReached(province, drawDate))
        {
            return cached; // Chưa đến giờ quay thưởng, trả về cache nếu có
        }

        var scraped = await ScrapeFromMinhNgocAsync(province, drawDate);
        if (scraped == null)
        {
            return cached; // Chưa có kết quả chính thức từ nhà đài, trả về cache nếu có
        }

        if (cached != null)
        {
            cached.Prizes = scraped.Prizes;
            await _repository.UpdateAsync(cached);
            return cached;
        }

        await _repository.InsertAsync(scraped);
        return scraped;
    }

    private async Task<LotteryDrawResult> ScrapeFromMinhNgocAsync(string province, DateTime drawDate)
    {
        var info = GetProvinceInfo(province);
        var regionStr = info.Region;
        var provSlug = info.Slug;
        var dateStr = drawDate.ToString("dd-MM-yyyy");

        string url;
        if (regionStr == "mien-bac")
        {
            url = $"https://www.minhngoc.net.vn/ket-qua-xo-so/mien-bac/{dateStr}.html";
        }
        else
        {
            if (string.IsNullOrEmpty(provSlug)) return null;
            url = $"https://www.minhngoc.net.vn/ket-qua-xo-so/{regionStr}/{provSlug}/{dateStr}.html";
        }
        
        Logger.Info($"Scraping {province} on {drawDate:yyyy-MM-dd} from URL: {url}");
        string html;
        try
        {
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
            };

            if (File.Exists("/usr/bin/chromium"))
            {
                launchOptions.ExecutablePath = "/usr/bin/chromium";
            }
            else if (File.Exists("/usr/bin/chromium-browser"))
            {
                launchOptions.ExecutablePath = "/usr/bin/chromium-browser";
            }
            else
            {
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
            }

            await using var browser = await Puppeteer.LaunchAsync(launchOptions);
            await using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
            html = await page.GetContentAsync();
            Logger.Info($"Successfully loaded HTML for {province} on {drawDate:yyyy-MM-dd}. Length: {html?.Length ?? 0}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error launching Puppeteer or loading page for {province} on {drawDate:yyyy-MM-dd} at URL {url}: {ex.Message}", ex);
            return null;
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new LotteryDrawResult
            {
                Province = province,
                DrawDate = drawDate
            };

            var prizesDict = new Dictionary<string, List<string>>();

            // 1. Tìm thẻ div.box_kqxs chứa kết quả của ngày mong muốn
            var boxNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'box_kqxs')]");
            HtmlNode targetBoxNode = null;
            if (boxNodes != null)
            {
                foreach (var box in boxNodes)
                {
                    var titleNode = box.SelectSingleNode(".//div[contains(@class, 'title')]");
                    if (titleNode != null)
                    {
                        var titleText = titleNode.InnerText;
                        var formattedDate1 = drawDate.ToString("dd/MM/yyyy");
                        var formattedDate2 = drawDate.ToString("dd-MM-yyyy");
                        if (titleText.Contains(formattedDate1) || titleText.Contains(formattedDate2))
                        {
                            targetBoxNode = box;
                            break;
                        }
                    }
                }
            }

            if (targetBoxNode == null && boxNodes != null && boxNodes.Count > 0)
            {
                targetBoxNode = boxNodes[0];
            }

            if (targetBoxNode == null)
            {
                targetBoxNode = doc.DocumentNode;
            }

            // 2. Xác định chỉ mục cột (column index) của tỉnh mong muốn trong bảng đa đài (nếu có)
            int colIndex = 0;
            var headerCells = targetBoxNode.SelectNodes(".//td[contains(concat(' ', normalize-space(@class), ' '), ' tinh ') or contains(concat(' ', normalize-space(@class), ' '), ' matinh ')]");
            if (headerCells != null && headerCells.Count > 0)
            {
                var provincesList = new List<string>();
                for (int i = 0; i < headerCells.Count; i++)
                {
                    provincesList.Add(headerCells[i].InnerText.Trim());
                }

                bool foundMatch = false;
                for (int i = 0; i < provincesList.Count; i++)
                {
                    if (IsTextMatchingProvince(provincesList[i], province))
                    {
                        colIndex = i;
                        foundMatch = true;
                        Logger.Info($"Detected column index {colIndex} for province '{province}' from headers: {string.Join(", ", provincesList)}");
                        break;
                    }
                }

                if (!foundMatch)
                {
                    Logger.Warn($"Province '{province}' not found in multi-province headers: {string.Join(", ", provincesList)}. Aborting parsing to prevent mismatch.");
                    return null;
                }
            }
            else
            {
                // Single-province page: We MUST verify that the page or target box title matches the requested province!
                var titleNode = targetBoxNode.SelectSingleNode(".//div[contains(@class, 'title')]");
                var boxTitle = titleNode?.InnerText ?? "";
                var docTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "";

                if (!IsTextMatchingProvince(boxTitle, province) && !IsTextMatchingProvince(docTitle, province))
                {
                    Logger.Warn($"Single-province page does not match requested province '{province}' (Box title: '{boxTitle}', Doc title: '{docTitle}'). Aborting parsing to prevent mismatch.");
                    return null;
                }

                colIndex = 0;
            }


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
                // Tìm kiếm tương đối bên trong thẻ box đã chọn
                var xpath = $".//td[contains(@class, '{kvp.Key}')]";
                var nodes = targetBoxNode.SelectNodes(xpath);
                Logger.Info($"Key {kvp.Key}: xpath '{xpath}' found {nodes?.Count ?? 0} nodes.");
                
                if (nodes != null && nodes.Count > 0)
                {
                    // Lọc lấy các node chứa kết quả trúng thưởng, tránh cột tiêu đề có đuôi 'l'
                    var validNodes = nodes.Where(n => {
                        var cls = n.Attributes["class"]?.Value?.Trim() ?? "";
                        return !cls.EndsWith("l") && !cls.Contains("title");
                    }).ToList();

                    if (validNodes.Count > 0)
                    {
                        // Chọn node tương ứng với chỉ mục cột đã phát hiện được
                        var node = colIndex < validNodes.Count ? validNodes[colIndex] : validNodes[0];

                    var clsVal = node.Attributes["class"]?.Value ?? "no-class";
                    Logger.Info($"Key {kvp.Key}: Selected node class: '{clsVal}', InnerText: '{node.InnerText.Trim()}'");

                    var numbers = new List<string>();
                    
                    // Thử bóc tách các div con chứa từng số trúng thưởng độc lập (tránh bị dính chữ số liền nhau)
                    var childDivs = node.SelectNodes(".//div");
                    if (childDivs != null && childDivs.Count > 0)
                    {
                        foreach (var div in childDivs)
                        {
                            var text = div.InnerText.Trim();
                            if (!string.IsNullOrEmpty(text) && text.All(char.IsDigit))
                            {
                                numbers.Add(text);
                            }
                        }
                        Logger.Info($"Key {kvp.Key}: Found {childDivs.Count} child divs. Extracted {numbers.Count} numbers.");
                    }

                    // Nếu không có div con hoặc bóc tách bằng div bị rỗng (ví dụ các số cách nhau bởi br hoặc text node trực tiếp)
                    if (!numbers.Any())
                    {
                        var text = node.InnerText.Trim();
                        var parts = text.Split(new[] { ' ', '\n', '\r', '-', '\t', '\u200B' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (part.All(char.IsDigit))
                            {
                                numbers.Add(part);
                            }
                        }
                        Logger.Info($"Key {kvp.Key}: Fallback parsing of InnerText '{text}'. Extracted {numbers.Count} numbers.");
                    }
                    
                    if (numbers.Any())
                    {
                        prizesDict[kvp.Value] = numbers.Distinct().ToList();
                    }
                    }
                }
            }

            result.Prizes = prizesDict;

            Logger.Info($"Parsing complete for {province} on {drawDate:yyyy-MM-dd}. Found {result.Prizes.Count} prizes.");
            if (!result.Prizes.Any())
            {
                var failedFilePath = $"/Users/tommy/.gemini/antigravity/brain/655dd85c-0dab-4c56-acf5-e258f67b5881/scratch/failed_{province.Replace(' ', '_').Replace('.', '_')}_{drawDate:yyyyMMdd}.html";
                File.WriteAllText(failedFilePath, html);
                Logger.Warn($"No prizes matched for {province} on {drawDate:yyyy-MM-dd} in the HTML. Saved HTML to {failedFilePath}");
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing HTML for {province} on {drawDate:yyyy-MM-dd}: {ex.Message}", ex);
            return null;
        }
    }

    public static readonly Dictionary<string, (string Region, string Slug)> ProvinceMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Miền Bắc
        { "miền bắc", ("mien-bac", "") },
        { "mien bac", ("mien-bac", "") },
        { "truyền thống", ("mien-bac", "") },
        { "truyen thong", ("mien-bac", "") },
        { "hà nội", ("mien-bac", "") },
        { "ha noi", ("mien-bac", "") },
        { "hải phòng", ("mien-bac", "") },
        { "hai phong", ("mien-bac", "") },
        { "quảng ninh", ("mien-bac", "") },
        { "quang ninh", ("mien-bac", "") },
        { "bắc ninh", ("mien-bac", "") },
        { "bac ninh", ("mien-bac", "") },
        { "nam định", ("mien-bac", "") },
        { "nam dinh", ("mien-bac", "") },
        { "thái bình", ("mien-bac", "") },
        { "thai binh", ("mien-bac", "") },

        // Miền Trung
        { "thừa thiên huế", ("mien-trung", "thua-thien-hue") },
        { "thua thien hue", ("mien-trung", "thua-thien-hue") },
        { "huế", ("mien-trung", "thua-thien-hue") },
        { "hue", ("mien-trung", "thua-thien-hue") },
        { "phú yên", ("mien-trung", "phu-yen") },
        { "phu yen", ("mien-trung", "phu-yen") },
        { "đắk lắk", ("mien-trung", "dak-lak") },
        { "dak lak", ("mien-trung", "dak-lak") },
        { "đắc lắc", ("mien-trung", "dak-lak") },
        { "dac lac", ("mien-trung", "dak-lak") },
        { "quảng nam", ("mien-trung", "quang-nam") },
        { "quang nam", ("mien-trung", "quang-nam") },
        { "đà nẵng", ("mien-trung", "da-nang") },
        { "da nang", ("mien-trung", "da-nang") },
        { "khánh hòa", ("mien-trung", "khanh-hoa") },
        { "khanh hoa", ("mien-trung", "khanh-hoa") },
        { "bình định", ("mien-trung", "binh-dinh") },
        { "binh dinh", ("mien-trung", "binh-dinh") },
        { "quảng trị", ("mien-trung", "quang-tri") },
        { "quang tri", ("mien-trung", "quang-tri") },
        { "quảng bình", ("mien-trung", "quang-binh") },
        { "quang binh", ("mien-trung", "quang-binh") },
        { "gia lai", ("mien-trung", "gia-lai") },
        { "ninh thuận", ("mien-trung", "ninh-thuan") },
        { "ninh thuan", ("mien-trung", "ninh-thuan") },
        { "đắk nông", ("mien-trung", "dak-nong") },
        { "dak nong", ("mien-trung", "dak-nong") },
        { "đắc nông", ("mien-trung", "dak-nong") },
        { "quảng ngãi", ("mien-trung", "quang-ngai") },
        { "quang ngai", ("mien-trung", "quang-ngai") },
        { "kon tum", ("mien-trung", "kon-tum") },

        // Miền Nam
        { "tp. hcm", ("mien-nam", "tp-hcm") },
        { "tp.hcm", ("mien-nam", "tp-hcm") },
        { "hồ chí minh", ("mien-nam", "tp-hcm") },
        { "ho chi minh", ("mien-nam", "tp-hcm") },
        { "tphcm", ("mien-nam", "tp-hcm") },
        { "sài gòn", ("mien-nam", "tp-hcm") },
        { "sai gon", ("mien-nam", "tp-hcm") },
        { "đồng tháp", ("mien-nam", "dong-thap") },
        { "dong thap", ("mien-nam", "dong-thap") },
        { "cà mau", ("mien-nam", "ca-mau") },
        { "ca mau", ("mien-nam", "ca-mau") },
        { "bến tre", ("mien-nam", "ben-tre") },
        { "ben tre", ("mien-nam", "ben-tre") },
        { "vũng tàu", ("mien-nam", "vung-tau") },
        { "vung tau", ("mien-nam", "vung-tau") },
        { "bạc liêu", ("mien-nam", "bac-lieu") },
        { "bac lieu", ("mien-nam", "bac-lieu") },
        { "đồng nai", ("mien-nam", "dong-nai") },
        { "dong nai", ("mien-nam", "dong-nai") },
        { "cần thơ", ("mien-nam", "can-tho") },
        { "can tho", ("mien-nam", "can-tho") },
        { "sóc trăng", ("mien-nam", "soc-trang") },
        { "soc trang", ("mien-nam", "soc-trang") },
        { "tây ninh", ("mien-nam", "tay-ninh") },
        { "tay ninh", ("mien-nam", "tay-ninh") },
        { "an giang", ("mien-nam", "an-giang") },
        { "bình thuận", ("mien-nam", "binh-thuan") },
        { "binh thuan", ("mien-nam", "binh-thuan") },
        { "vĩnh long", ("mien-nam", "vinh-long") },
        { "vinh long", ("mien-nam", "vinh-long") },
        { "bình dương", ("mien-nam", "binh-duong") },
        { "binh duong", ("mien-nam", "binh-duong") },
        { "trà vinh", ("mien-nam", "tra-vinh") },
        { "tra vinh", ("mien-nam", "tra-vinh") },
        { "long an", ("mien-nam", "long-an") },
        { "bình phước", ("mien-nam", "binh-phuoc") },
        { "binh phuoc", ("mien-nam", "binh-phuoc") },
        { "hậu giang", ("mien-nam", "hau-giang") },
        { "hau giang", ("mien-nam", "hau-giang") },
        { "tiền giang", ("mien-nam", "tien-giang") },
        { "tien giang", ("mien-nam", "tien-giang") },
        { "kiên giang", ("mien-nam", "kien-giang") },
        { "kien giang", ("mien-nam", "kien-giang") },
        { "đà lạt", ("mien-nam", "da-lat") },
        { "da lat", ("mien-nam", "da-lat") },
        { "lâm đồng", ("mien-nam", "da-lat") },
        { "lam dong", ("mien-nam", "da-lat") }
    };

    public static (string Region, string Slug) GetProvinceInfo(string province)
    {
        if (string.IsNullOrEmpty(province)) return ("mien-nam", "tp-hcm");
        
        var p = province.ToLower();
        foreach (var kvp in ProvinceMappings)
        {
            if (p.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        
        return ("mien-nam", "tp-hcm");
    }

    private static bool IsTextMatchingProvince(string text, string province)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(province)) return false;

        var info = GetProvinceInfo(province);
        var synonyms = ProvinceMappings
            .Where(kvp => kvp.Value.Region == info.Region && kvp.Value.Slug == info.Slug)
            .Select(kvp => kvp.Key)
            .ToList();

        synonyms.Add(province);

        var lowerText = text.ToLower();
        foreach (var syn in synonyms)
        {
            if (lowerText.Contains(syn.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    private string GetRegionFromProvince(string province)
    {
        return GetProvinceInfo(province).Region;
    }

    public static bool IsProvinceActiveOnDayOfWeek(string province, DayOfWeek dayOfWeek)
    {
        var p = province.ToLower();
        
        // Miền Bắc quay hàng ngày
        if (p.Contains("miền bắc") || p.Contains("mien bac") || p.Contains("truyền thống") || p.Contains("truyen thong"))
            return true;

        switch (dayOfWeek)
        {
            case DayOfWeek.Monday:
                return p.Contains("thừa thiên huế") || p.Contains("huế") || p.Contains("thua thien hue") || p.Contains("hue") ||
                       p.Contains("phú yên") || p.Contains("phu yen") ||
                       p.Contains("tp. hcm") || p.Contains("hồ chí minh") || p.Contains("tphcm") || p.Contains("sài gòn") ||
                       p.Contains("đồng tháp") || p.Contains("dong thap") ||
                       p.Contains("cà mau") || p.Contains("ca mau");

            case DayOfWeek.Tuesday:
                return p.Contains("đắk lắk") || p.Contains("dak lak") || p.Contains("đắc lắc") ||
                       p.Contains("quảng nam") || p.Contains("quang nam") ||
                       p.Contains("bến tre") || p.Contains("ben tre") ||
                       p.Contains("vũng tàu") || p.Contains("vung tau") ||
                       p.Contains("bạc liêu") || p.Contains("bac lieu");

            case DayOfWeek.Wednesday:
                return p.Contains("đà nẵng") || p.Contains("da nang") ||
                       p.Contains("khánh hòa") || p.Contains("khanh hoa") ||
                       p.Contains("đồng nai") || p.Contains("dong nai") ||
                       p.Contains("cần thơ") || p.Contains("can tho") ||
                       p.Contains("sóc trăng") || p.Contains("soc trang");

            case DayOfWeek.Thursday:
                return p.Contains("bình định") || p.Contains("binh dinh") ||
                       p.Contains("quảng trị") || p.Contains("quang tri") ||
                       p.Contains("quảng bình") || p.Contains("quang binh") ||
                       p.Contains("tây ninh") || p.Contains("tay ninh") ||
                       p.Contains("an giang") ||
                       p.Contains("bình thuận") || p.Contains("binh thuan");

            case DayOfWeek.Friday:
                return p.Contains("gia lai") ||
                       p.Contains("ninh thuận") || p.Contains("ninh thuan") ||
                       p.Contains("vĩnh long") || p.Contains("vinh long") ||
                       p.Contains("bình dương") || p.Contains("binh duong") ||
                       p.Contains("trà vinh") || p.Contains("tra vinh");

            case DayOfWeek.Saturday:
                return p.Contains("đà nẵng") || p.Contains("da nang") ||
                       p.Contains("quảng ngãi") || p.Contains("quang ngai") ||
                       p.Contains("đắk nông") || p.Contains("dak nong") || p.Contains("đắc nông") ||
                       p.Contains("tp. hcm") || p.Contains("hồ chí minh") || p.Contains("tphcm") || p.Contains("sài gòn") ||
                       p.Contains("long an") ||
                       p.Contains("hậu giang") || p.Contains("hau giang") ||
                       p.Contains("bình phước") || p.Contains("binh phuoc");

            case DayOfWeek.Sunday:
                return p.Contains("khánh hòa") || p.Contains("khanh hoa") ||
                       p.Contains("kon tum") ||
                       p.Contains("thừa thiên huế") || p.Contains("huế") || p.Contains("thua thien hue") || p.Contains("hue") ||
                       p.Contains("tiền giang") || p.Contains("tien giang") ||
                       p.Contains("kiên giang") || p.Contains("kien giang") ||
                       p.Contains("đà lạt") || p.Contains("da lat") || p.Contains("lâm đồng") || p.Contains("lam dong");
        }

        return false;
    }
}

