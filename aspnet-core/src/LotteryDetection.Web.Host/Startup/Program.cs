using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace LotteryDetection.Web.Startup;

public class Program
{
    public static void Main(string[] args)
    {
        // ABP audit columns (CreationTime, etc.) write DateTime.Now (Kind=Local).
        // Npgsql 6+ rejects non-UTC values for timestamptz; this switch restores
        // the pre-6 behavior of treating any DateTime as the column's TZ.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return new WebHostBuilder()
            .UseKestrel(opt =>
            {
                opt.AddServerHeader = false;
                opt.Limits.MaxRequestLineSize = 16 * 1024;
            })
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureLogging((context, logging) =>
            {
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            })
            .UseIIS()
            .UseIISIntegration()
            .UseStartup<Startup>();
    }
}