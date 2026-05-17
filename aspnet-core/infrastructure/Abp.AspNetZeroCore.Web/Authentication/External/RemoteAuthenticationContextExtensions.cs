using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public static class RemoteAuthenticationContextExtensions
{
    public static void AddMappedClaims<TOptions>(
        this RemoteAuthenticationContext<TOptions> context,
        List<JsonClaimMap> mappings)
        where TOptions : RemoteAuthenticationOptions
    {
        if (!mappings.Any())
            return;
        foreach (var mapping in mappings)
        {
            var claimMapping = mapping;
            var claim = context.Principal.Claims.ToList().FirstOrDefault(c => c.Type == claimMapping.Key);
            if (claim != null)
                context.Principal.AddIdentity(new ClaimsIdentity(new List<Claim>
                {
                    new(claimMapping.Claim, claim.Value)
                }));
        }
    }

    public static void AddMappedClaims(this ClaimsPrincipal principal, List<JsonClaimMap> mappings)
    {
        if (!mappings.Any())
            return;
        foreach (var mapping in mappings)
        {
            var claimMapping = mapping;
            var claim = principal.Claims.ToList().FirstOrDefault(c => c.Type == claimMapping.Key);
            if (claim != null)
                principal.AddIdentity(new ClaimsIdentity(new List<Claim>
                {
                    new(claimMapping.Claim, claim.Value)
                }));
        }
    }
}