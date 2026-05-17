using System.Threading.Tasks;
using LotteryDetection.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LotteryDetection.Web.Controllers;

public class BrowserCacheCleanerController : LotteryDetectionControllerBase
{
    private readonly INotificationAppService _notificationAppService;

    public BrowserCacheCleanerController(INotificationAppService notificationAppService)
    {
        _notificationAppService = notificationAppService;
    }

    public async Task<IActionResult> Clear()
    {
        var result = await _notificationAppService.SetAllAvailableVersionNotificationAsRead();

        HttpContext.Response.Headers.Append("Clear-Site-Data", "\"cache\"");

        return Json(new { Result = result });
    }
}