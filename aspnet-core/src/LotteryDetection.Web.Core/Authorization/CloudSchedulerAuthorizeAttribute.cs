using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class CloudSchedulerAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string HeaderName = "X-CloudScheduler-Job-Key";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["CloudScheduler:JobKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            // If not configured, we allow it (or you can block it. Usually block is safer, but for local dev allow might be easier)
            // Let's block if missing to be safe in production.
            context.Result = new UnauthorizedObjectResult("Cloud Scheduler API Key is not configured on the server.");
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey))
        {
            context.Result = new UnauthorizedObjectResult($"Missing {HeaderName} header.");
            return;
        }

        if (!string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult("Invalid Cloud Scheduler API Key.");
            return;
        }

        await Task.CompletedTask;
    }
}
