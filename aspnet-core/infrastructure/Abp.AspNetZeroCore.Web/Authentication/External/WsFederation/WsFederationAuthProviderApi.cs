using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;

namespace Abp.AspNetZeroCore.Web.Authentication.External.WsFederation;

public class WsFederationAuthProviderApi : ExternalAuthProviderApiBase
{
    public const
        string Name = "WsFederation";

    public override async Task<ExternalAuthUserInfo> GetUserInfo(string token)
    {
        var issuer = ProviderInfo.AdditionalParams["Authority"];
        if (string.IsNullOrEmpty(issuer))
            throw new ApplicationException("Authentication:WsFederation:Issuer configuration is required.");
        var metadata = ProviderInfo.AdditionalParams["MetaDataAddress"];
        if (string.IsNullOrEmpty(issuer))
            throw new ApplicationException("Authentication:WsFederation:MetaDataAddress configuration is required.");
        var configurationManager = new ConfigurationManager<WsFederationConfiguration>(metadata,
            new WsFederationConfigurationRetriever(), new HttpDocumentRetriever());
        var validatedToken = await ValidateToken(token, issuer, configurationManager);
        var fullName = validatedToken.Claims.First(c => c.Type == "name").Value;
        var email = validatedToken.Claims.First(c => c.Type == "email").Value;
        var fullNameParts = fullName.Split(' ');
        var result = new ExternalAuthUserInfo
        {
            Provider = "WsFederation",
            ProviderKey = validatedToken.Subject,
            Name = fullNameParts[0],
            Surname = fullNameParts.Length > 1 ? fullNameParts[1] : fullNameParts[0],
            EmailAddress = email,
            Claims = validatedToken.Claims.Select(c => new ClaimKeyValue(c.Type, c.Value)).ToList()
        };
        return result;
    }

    private async Task<JwtSecurityToken> ValidateToken(
        string token,
        string issuer,
        IConfigurationManager<WsFederationConfiguration> configurationManager,
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
            ValidateIssuer = true,
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
        if (ProviderInfo.ClientId != principal.Claims.First(c => c.Type == "aud").Value)
            throw new ApplicationException("ClientId couldn't verified.");
        var jwtSecurityToken = (JwtSecurityToken)rawValidatedToken;
        return jwtSecurityToken;
    }
}