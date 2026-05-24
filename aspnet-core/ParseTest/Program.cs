using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== STARTING ROBUST PARSING TEST ===");

        // Test Case 1: Miền Trung (Multi-province: Đà Nẵng, Quảng Ngãi, Đắk Nông)
        string daNangHtmlPath = "/Users/tommy/.gemini/antigravity/brain/655dd85c-0dab-4c56-acf5-e258f67b5881/scratch/test_da_nang.html";
        TestParse(daNangHtmlPath, "Đà Nẵng", new DateTime(2026, 05, 23));
        TestParse(daNangHtmlPath, "Quảng Ngãi", new DateTime(2026, 05, 23));
        TestParse(daNangHtmlPath, "Đắk Nông", new DateTime(2026, 05, 23));

        // Test Case 1b: Failed Quảng Ngãi
        string failedQuangNgaiPath = "/Users/tommy/.gemini/antigravity/brain/655dd85c-0dab-4c56-acf5-e258f67b5881/scratch/failed_Quảng_Ngãi_20260523.html";
        TestParse(failedQuangNgaiPath, "Quảng Ngãi", new DateTime(2026, 05, 23));

        // Test Case 2: Miền Bắc
        string mienBacHtmlPath = "/Users/tommy/.gemini/antigravity/brain/655dd85c-0dab-4c56-acf5-e258f67b5881/scratch/test_mien_bac.html";
        TestParse(mienBacHtmlPath, "Miền Bắc", new DateTime(2026, 05, 24));
    }

    static void TestParse(string htmlPath, string province, DateTime drawDate)
    {
        Console.WriteLine($"\n--- Testing parsing for '{province}' from file: {Path.GetFileName(htmlPath)} ---");
        if (!File.Exists(htmlPath))
        {
            Console.WriteLine($"Error: File not found at {htmlPath}");
            return;
        }

        string html = File.ReadAllText(htmlPath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 1. Find box_kqxs block
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

        // 2. Column index detection
        int colIndex = 0;
        var headerCells = targetBoxNode.SelectNodes(".//td[contains(concat(' ', normalize-space(@class), ' '), ' tinh ') or contains(concat(' ', normalize-space(@class), ' '), ' matinh ')]");
        if (headerCells != null && headerCells.Count > 0)
        {
            var provincesList = new List<string>();
            for (int i = 0; i < headerCells.Count; i++)
            {
                provincesList.Add(headerCells[i].InnerText.Trim());
            }

            Console.WriteLine($"Found multi-province headers in HTML: {string.Join(" | ", provincesList)}");

            bool foundMatch = false;
            for (int i = 0; i < provincesList.Count; i++)
            {
                if (IsTextMatchingProvince(provincesList[i], province))
                {
                    colIndex = i;
                    foundMatch = true;
                    Console.WriteLine($"=> Detected column index {colIndex} for province '{province}'");
                    break;
                }
            }

            if (!foundMatch)
            {
                Console.WriteLine($"WARNING: Province '{province}' not found in multi-province headers. Skipping/Aborting to prevent mismatch!");
                return;
            }
        }
        else
        {
            // Single province page: We MUST verify the page/box title matches the requested province!
            var titleNode = targetBoxNode.SelectSingleNode(".//div[contains(@class, 'title')]");
            var boxTitle = titleNode?.InnerText ?? "";
            var docTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "";

            if (!IsTextMatchingProvince(boxTitle, province) && !IsTextMatchingProvince(docTitle, province))
            {
                Console.WriteLine($"WARNING: Single-province page does not match requested province '{province}' (Box title: '{boxTitle}', Doc title: '{docTitle}'). Skipping/Aborting!");
                return;
            }

            colIndex = 0;
        }

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

        var prizesDict = new Dictionary<string, List<string>>();

        foreach (var kvp in classMap)
        {
            var xpath = $".//td[contains(@class, '{kvp.Key}')]";
            var nodes = targetBoxNode.SelectNodes(xpath);
            
            if (nodes != null && nodes.Count > 0)
            {
                var validNodes = nodes.Where(n => {
                    var cls = n.Attributes["class"]?.Value?.Trim() ?? "";
                    return !cls.EndsWith("l") && !cls.Contains("title");
                }).ToList();

                if (validNodes.Count > 0)
                {
                    var node = colIndex < validNodes.Count ? validNodes[colIndex] : validNodes[0];
                    var numbers = new List<string>();
                    
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
                        prizesDict[kvp.Value] = numbers.Distinct().ToList();
                    }
                }
            }
        }

        // Print final parsed results
        if (prizesDict.Count > 0)
        {
            Console.WriteLine($"SUCCESS: Found {prizesDict.Count} prize tiers for {province}:");
            foreach (var kvp in prizesDict)
            {
                Console.WriteLine($"  - {kvp.Key}: {string.Join(" · ", kvp.Value)}");
            }
        }
        else
        {
            Console.WriteLine("FAILURE: No prizes parsed!");
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
}
