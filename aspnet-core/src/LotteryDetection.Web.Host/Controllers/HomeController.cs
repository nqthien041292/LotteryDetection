using Abp.Auditing;
using LotteryDetection.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LotteryDetection.Web.Controllers;

public class HomeController : LotteryDetectionControllerBase
{
    private readonly IConfigurationRoot _appConfiguration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HomeController(
        IWebHostEnvironment webHostEnvironment,
        IAppConfigurationAccessor appConfigurationAccessor)
    {
        _webHostEnvironment = webHostEnvironment;
        _appConfiguration = appConfigurationAccessor.Configuration;
    }

    [DisableAuditing]
    public IActionResult Index()
    {
        if (_webHostEnvironment.IsDevelopment()) return RedirectToAction("Index", "Ui");

        var homePageUrl = _appConfiguration["App:HomePageUrl"];
        if (string.IsNullOrEmpty(homePageUrl)) return RedirectToAction("Index", "Ui");

        return Redirect(homePageUrl);
    }
}