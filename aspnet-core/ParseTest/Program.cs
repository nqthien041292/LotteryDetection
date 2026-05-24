using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

class Program
{
    static void Main()
    {
        string htmlPath = "/Users/tommy/.gemini/antigravity/brain/655dd85c-0dab-4c56-acf5-e258f67b5881/scratch/test_da_nang.html";
        string html = File.ReadAllText(htmlPath);

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

        Console.WriteLine("--- Starting Parsing Test ---");
        foreach (var kvp in classMap)
        {
            var xpath = $"//td[contains(@class, '{kvp.Key}')]";
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            
            if (nodes == null)
            {
                Console.WriteLine($"Key {kvp.Key}: SelectNodes returned NULL for xpath: {xpath}");
                continue;
            }

            Console.WriteLine($"Key {kvp.Key}: Found {nodes.Count} nodes.");
            
            // Thử logic lọc node
            var node = nodes.FirstOrDefault(n => {
                var cls = n.Attributes["class"]?.Value?.Trim() ?? "";
                return !cls.EndsWith("l") && !cls.Contains("title");
            }) ?? (nodes.Count > 1 ? nodes[1] : nodes[0]);

            if (node == null)
            {
                Console.WriteLine($"Key {kvp.Key}: Selected node is NULL!");
                continue;
            }

            var clsValue = node.Attributes["class"]?.Value ?? "no-class";
            Console.WriteLine($"Key {kvp.Key}: Selected node class: '{clsValue}', InnerText: '{node.InnerText.Trim()}'");

            var numbers = new List<string>();
            var childDivs = node.SelectNodes(".//div");
            if (childDivs != null && childDivs.Count > 0)
            {
                Console.WriteLine($"Key {kvp.Key}: Found {childDivs.Count} child divs.");
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

            Console.WriteLine($"Key {kvp.Key}: Extracted {numbers.Count} numbers: {string.Join(", ", numbers)}");
        }
    }
}
