using LotteryDetection.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace LotteryDetection.Web.Public.Controllers;

public class HomeController : LotteryDetectionControllerBase
{
    public ActionResult Index()
    {
        return View();
    }

    public ActionResult Privacy()
    {
        return View();
    }
}