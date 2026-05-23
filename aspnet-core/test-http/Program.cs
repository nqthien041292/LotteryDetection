using System;
using System.Threading.Tasks;
using PuppeteerSharp;
using HtmlAgilityPack;

class Program
{
    static async Task Main()
    {
        var url = "https://www.minhngoc.net.vn/ket-qua-xo-so/mien-nam/an-giang/21-05-2026.html";
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        
        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
        };

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
        var html = await page.GetContentAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var nodes = doc.DocumentNode.SelectNodes("//td[contains(@class, 'giaidb')]");
        if (nodes != null && nodes.Count > 1) {
            Console.WriteLine($"Found Data: {nodes[1].InnerText.Trim()}");
        } else {
            Console.WriteLine("Could not find the node.");
        }
    }
}
