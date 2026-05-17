using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.UI;
using Castle.Core.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Abp.AspNetZeroCore.Web.Authentication.External.OpenIdConnect;

public class OpenIdConnectAuthProviderApi : ExternalAuthProviderApiBase
{
    public const
        string Name = "OpenIdConnect";

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public override async Task<ExternalAuthUserInfo> GetUserInfo(string token)
    {
        var issuer = ProviderInfo.AdditionalParams["Authority"];
        Logger.Info("Using " + issuer + " as issuer for OpenID Connect");
        var configurationManager = !string.IsNullOrEmpty(issuer)
            ? new ConfigurationManager<OpenIdConnectConfiguration>(
                issuer.EnsureEndsWith('/') + ".well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever())
            : throw new ApplicationException("Authentication:OpenId:Issuer configuration is required.");
        Logger.Info("Validating retrieved token for OpenID Connect");
        var validatedTokenResult = await ValidateToken(token, issuer, configurationManager);
        Logger.Info("OpenID Connect token is validated");
        Logger.Info("Retrieved claims:");
        foreach (var claim in validatedTokenResult.Principal.Claims)
            Logger.Info(claim.Type + " -> " + claim.Value);
        var fullNameClaim = GetFullNameClaim(validatedTokenResult);
        var emailClaim = GetEmailClaim(validatedTokenResult);
        var fullNameParts = fullNameClaim.Value.Split(' ');
        var result = new ExternalAuthUserInfo
        {
            Provider = "OpenIdConnect",
            ProviderKey = validatedTokenResult.Token.Subject,
            Name = fullNameParts[0],
            Surname = fullNameParts.Length > 1 ? fullNameParts[1] : fullNameParts[0],
            EmailAddress = emailClaim.Value,
            Claims = validatedTokenResult.Principal.Claims.Select(c => new ClaimKeyValue(c.Type, c.Value)).ToList()
        };
        return result;
    }

    private Claim GetFullNameClaim(
        ValidateTokenResult validatedTokenResult)
    {
        var fullNameClaim = validatedTokenResult.Principal.Claims.FirstOrDefault(c => c.Type == "name");
        if (fullNameClaim != null)
            return fullNameClaim;
        Logger.Warn(
            "name claim is missing! You can use claims mapping to map one of the retrieved claims to name claim.");
        Logger.Info("Looking for http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name claim");
        return validatedTokenResult.Principal.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name") ?? throw new UserFriendlyException(
            "Both name and http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name claims are missing! You can use claims mapping to map one of the retrieved claims to name or http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name claim.");
    }

    private Claim GetEmailClaim(
        ValidateTokenResult validatedTokenResult)
    {
        var emailClaim = validatedTokenResult.Principal.Claims.FirstOrDefault(c => c.Type == "unique_name");
        if (emailClaim != null)
            return emailClaim;
        Logger.Warn(
            "unique_name claim is missing! You can use claims mapping to map one of the retrieved claims to unique_name claim.");
        Logger.Info("Looking for http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress claim");
        return validatedTokenResult.Principal.Claims.FirstOrDefault(c =>
                   c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress") ??
               throw new UserFriendlyException(
                   "Both name and http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress claims are missing! You can use claims mapping to map one of the retrieved claims to name or http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress claim.");
    }

    private async Task<ValidateTokenResult> ValidateToken(
        string token,
        string issuer,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrEmpty(issuer))
            throw new ArgumentNullException(nameof(issuer));
        var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);
        var signingKeys = discoveryDocument.SigningKeys;
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = bool.Parse(ProviderInfo.AdditionalParams["ValidateIssuer"]),
            ValidIssuer = issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5.0),
            ValidateAudience = false
        };
        var principal =
            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out var rawValidatedToken);
        principal.AddMappedClaims(ProviderInfo.ClaimMappings);
        var audienceClaim = principal.Claims.FirstOrDefault(c => c.Type == "aud") ??
                            throw new UserFriendlyException(
                                "aud claim is missing ! You can use claims mapping to map one of the retrieved claims to aud claim.");
        if (ProviderInfo.ClientId != audienceClaim.Value)
            throw new ApplicationException("ClientId couldn't verified.");
        var validateTokenResult = new ValidateTokenResult((JwtSecurityToken)rawValidatedToken, principal);
        return validateTokenResult;
    }

    private class ValidateTokenResult
    {
        public ValidateTokenResult(JwtSecurityToken token, ClaimsPrincipal principal)
        {
            Token = token;
            Principal = principal;
        }

        public JwtSecurityToken Token { get; }

        public ClaimsPrincipal Principal { get; }
    }
}