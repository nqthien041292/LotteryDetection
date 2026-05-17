using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Abp.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Facebook;

public class FacebookAuthProviderApi : ExternalAuthProviderApiBase
{
    public const
        string Name = "Facebook";

    public override async Task<ExternalAuthUserInfo> GetUserInfo(string accessCode)
    {
        var endpoint = QueryHelpers.AddQueryString("https://graph.facebook.com/v2.8/me", "access_token", accessCode);
        endpoint = QueryHelpers.AddQueryString(endpoint, "appsecret_proof", GenerateAppSecretProof(accessCode));
        endpoint = QueryHelpers.AddQueryString(endpoint, "fields", "email,last_name,first_name,middle_name");
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OAuth middleware");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Host = "graph.facebook.com";
        client.Timeout = TimeSpan.FromSeconds(30.0);
        client.MaxResponseContentBufferSize = 10485760L;
        var response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var str = await response.Content.ReadAsStringAsync();
        var payload = JObject.Parse(str);
        var name = FacebookHelper.GetFirstName(payload);
        var middleName = FacebookHelper.GetMiddleName(payload);
        if (!middleName.IsNullOrEmpty())
            name += middleName;
        var result = new ExternalAuthUserInfo
        {
            Name = name,
            EmailAddress = FacebookHelper.GetEmail(payload),
            Surname = FacebookHelper.GetLastName(payload),
            Provider = "Facebook",
            ProviderKey = FacebookHelper.GetId(payload)
        };
        FillClaimsFromJObject(result, payload);
        return result;
    }

    private string GenerateAppSecretProof(string accessToken)
    {
        using var hmacshA256 = new HMACSHA256(Encoding.ASCII.GetBytes(ProviderInfo.ClientSecret));
        var hash = hmacshA256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
        var stringBuilder = new StringBuilder();
        foreach (var t in hash)
            stringBuilder.Append(t.ToString("x2", CultureInfo.InvariantCulture));

        return stringBuilder.ToString();
    }
}