using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Microsoft;

public class MicrosoftAuthProviderApi : ExternalAuthProviderApiBase
{
    public const
        string Name = "Microsoft";

    public override async Task<ExternalAuthUserInfo> GetUserInfo(string accessCode)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OAuth middleware");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(30.0);
        client.MaxResponseContentBufferSize = 10485760L;
        var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, MicrosoftAccountDefaults.UserInformationEndpoint)
            {
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", accessCode)
                }
            });
        response.EnsureSuccessStatusCode();
        var str = await response.Content.ReadAsStringAsync();
        var payload = JObject.Parse(str);
        var result = new ExternalAuthUserInfo
        {
            Name = MicrosoftAccountHelper.GetGivenName(payload),
            EmailAddress = MicrosoftAccountHelper.GetEmail(payload),
            Surname = MicrosoftAccountHelper.GetSurname(payload),
            Provider = "Microsoft",
            ProviderKey = MicrosoftAccountHelper.GetId(payload)
        };
        FillClaimsFromJObject(result, payload);
        return result;
    }
}