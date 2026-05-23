using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        try
        {
            var html = await client.GetStringAsync("https://www.minhngoc.net.vn/ket-qua-xo-so/mien-nam/an-giang/21-05-2026.html");
            Console.WriteLine("Success, length: " + html.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed: " + ex.Message);
        }
    }
}
