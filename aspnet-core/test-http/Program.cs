using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PuppeteerSharp;
using HtmlAgilityPack;

class Program
{
    static readonly Dictionary<string, (string Region, string Slug)> ProvinceMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Miền Bắc", ("mien-bac", "") },
        { "Khánh Hòa", ("mien-trung", "khanh-hoa") },
        { "TP. HCM", ("mien-nam", "tp-hcm") }
    };

    static async Task Main()
    {
        Console.WriteLine(">>> KÍCH HOẠT CÀO THỬ NGHIỆM 3 MIỀN BẰNG PUPPETEER SHARP...");
        
        var results = new List<object>();
        var targetDate = DateTime.UtcNow.AddHours(7).AddDays(-1); // Ngày hôm qua
        var dateStr = targetDate.ToString("dd-MM-yyyy");

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
        };

        try
        {
            Console.WriteLine("- Đang khởi động Puppeteer...");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(launchOptions);
            await using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            foreach (var kvp in ProvinceMappings)
            {
                var province = kvp.Key;
                var region = kvp.Value.Region;
                var slug = kvp.Value.Slug;

                string url = region == "mien-bac" 
                    ? $"https://www.minhngoc.net.vn/ket-qua-xo-so/mien-bac/{dateStr}.html"
                    : $"https://www.minhngoc.net.vn/ket-qua-xo-so/{region}/{slug}/{dateStr}.html";

                Console.WriteLine($"\n-> Đang cào đài [{province}] ngày [{dateStr}]...");
                Console.WriteLine($"   URL: {url}");

                try
                {
                    await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
                    var html = await page.GetContentAsync();

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

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

                    var prizes = new Dictionary<string, List<string>>();

                    foreach (var prizeKvp in classMap)
                    {
                        var nodes = doc.DocumentNode.SelectNodes($"//td[contains(@class, '{prizeKvp.Key}')]");
                        if (nodes != null && nodes.Count > 1)
                        {
                            var node = nodes[1];
                            var numbers = new List<string>();

                            // Thử bóc tách các div con chứa từng số trúng thưởng
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
                            }

                            if (numbers.Any())
                            {
                                prizes[prizeKvp.Value] = numbers.Distinct().ToList();
                            }
                        }
                    }

                    if (prizes.Any())
                    {
                        Console.WriteLine($"   SUCCESS: Đã cào thành công {prizes.Count} hạng giải!");
                        results.Add(new
                        {
                            Province = province,
                            DrawDate = dateStr,
                            Prizes = prizes
                        });
                    }
                    else
                    {
                        Console.WriteLine("   WARNING: Không tìm thấy kết quả hoặc đài này không quay thưởng vào ngày hôm qua.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ERROR: Lỗi khi cào đài {province}: {ex.Message}");
                }
            }

            // Ghi kết quả ra file JSON local
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "scraped_results_sample.json");
            var jsonString = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonPath, jsonString);

            Console.WriteLine($"\n>>> HOÀN TẤT! Dữ liệu mẫu đã được ghi vào: {jsonPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> LỖI KHỞI ĐỘNG PUPPETEER: {ex.Message}");
        }
    }
}
