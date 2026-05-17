using Microsoft.AspNetCore.Mvc;
using LotteryDetection.Web.Controllers;

namespace LotteryDetection.Web.Public.Controllers;

public class HomeController : LotteryDetectionControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}

