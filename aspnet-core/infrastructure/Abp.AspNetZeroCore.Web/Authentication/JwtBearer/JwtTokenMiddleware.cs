using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;

namespace Abp.AspNetZeroCore.Web.Authentication.JwtBearer;

public static class JwtTokenMiddleware
{
    public static IApplicationBuilder UseJwtTokenMiddleware(this IApplicationBuilder app, string schema = "Bearer")
    {
        return app.Use(async (ctx, next) =>
        {
            var identity = ctx.User.Identity;
            if (identity is not { IsAuthenticated: true })
            {
                var result = await ctx.AuthenticateAsync(schema);
                if (result.Succeeded && result.Principal != null)
                    ctx.User = result.Principal;
                // ReSharper disable once RedundantAssignment
                result = null;
            }

            await next();
        });
    }
}