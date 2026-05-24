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
