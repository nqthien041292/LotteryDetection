using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LotteryDetection.Web.Startup;

public class AppKeyValidationMiddleware
{
    private readonly RequestDelegate _next;
    private const string AppKeyHeaderName = "X-App-Key";
    private const string ExpectedAppKey = "K#9p@mQ$zX7&rV!2wT*8yP%3bN^6vC$5";

    public AppKeyValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Only validate API requests
        if (path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase))
        {
            // Cloud Scheduler hits CheckPendingResults / similar endpoints with its
            // own X-CloudScheduler-Job-Key header, validated downstream by the
            // [CloudSchedulerAuthorize] filter. It doesn't carry the mobile app key,
            // so let those requests through and rely on the filter to authenticate.
            if (context.Request.Headers.ContainsKey("X-CloudScheduler-Job-Key"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(AppKeyHeaderName, out var extractedAppKey) ||
                extractedAppKey != ExpectedAppKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Unauthorized. Access is restricted to DoVeSo AI App only.\"}");
                return;
            }
        }

        await _next(context);
    }
}
