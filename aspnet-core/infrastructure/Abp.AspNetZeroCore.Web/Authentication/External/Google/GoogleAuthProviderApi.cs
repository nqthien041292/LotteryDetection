using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Google;

public class GoogleAuthProviderApi : ExternalAuthProviderApiBase
{
    public const
        string Name = "Google";

    public override async Task<ExternalAuthUserInfo> GetUserInfo(string accessCode)
    {
        var userInfoEndpoint = ProviderInfo.AdditionalParams["UserInfoEndpoint"];
        if (string.IsNullOrEmpty(userInfoEndpoint))
            throw new ApplicationException("Authentication:Google:UserInfoEndpoint configuration is required.");
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OAuth middleware");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(30.0);
        client.MaxResponseContentBufferSize = 10485760L;
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint)
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
            Name = GoogleHelper.GetName(payload),
            EmailAddress = GoogleHelper.GetEmail(payload),
            Surname = GoogleHelper.GetFamilyName(payload),
            ProviderKey = GoogleHelper.GetId(payload),
            Provider = "Google"
        };
        FillClaimsFromJObject(result, payload);

        return result;
    }
}