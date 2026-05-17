using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.UI;
using Castle.Core.Logging;
using Microsoft.AspNetCore.WebUtilities;
using Tweetinvi;
using HttpMethod = System.Net.Http.HttpMethod;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterAuthProviderApi : ExternalAuthProviderApiBase
{
    public const string Name = "Twitter";
    private const string BaseApiUrl = "https://api.twitter.com/";

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public async Task<TwitterGetRequestTokenResponse> GetRequestToken(
        string apiKey,
        string apiKeySecret,
        string callbackUrl)
    {
        var endpoint = "https://api.twitter.com/".EnsureEndsWith('/') + "oauth/request_token";
        if (!QueryHelpers.ParseQuery(callbackUrl).ContainsKey("twitter"))
            callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "twitter", "1");
        var twitterGetTokenRequest = new TwitterGetTokenRequest
        {
            ConsumerKey = apiKey,
            CallbackUrl = callbackUrl
        };
        var parameterString = twitterGetTokenRequest.GetParameterString("&");
        var base64Signature =
            new TwitterSignatureHelper().GetBase64Signature(endpoint, "POST", parameterString, apiKeySecret);
        var authenticationHeaderValue = "oauth_signature=\"" + WebUtility.UrlEncode(base64Signature) + "\", " +
                                        twitterGetTokenRequest.GetParameterString(",");
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(new Uri("https://api.twitter.com/"), endpoint)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authenticationHeaderValue);
        TwitterGetRequestTokenResponse requestToken;
        using var httpResponseMessage = await new HttpClient().SendAsync(request);
        var response = await httpResponseMessage.Content.ReadAsStringAsync();
        if (httpResponseMessage.IsSuccessStatusCode)
        {
            var requestTokenResponse = new TwitterGetRequestTokenResponse(response);
            requestTokenResponse.RedirectUrl = "https://api.twitter.com/".EnsureEndsWith('/') +
                                               "oauth/authenticate?oauth_token=" + requestTokenResponse.Token;
            requestToken = requestTokenResponse;
        }
        else
        {
            Logger.Error("Twitter API error: " + response);
            throw new UserFriendlyException("Can't connect to twitter.");
        }

        return requestToken;
    }

    public async Task<TwitterGetAccessTokenResponse> GetAccessToken(string token, string verifier)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(new Uri("https://api.twitter.com/"),
                "/oauth/access_token?oauth_token=" + WebUtility.UrlEncode(token) + "&oauth_verifier=" +
                WebUtility.UrlEncode(verifier))
        };
        TwitterGetAccessTokenResponse accessToken;
        using var httpResponseMessage = await new HttpClient().SendAsync(request);
        var response = await httpResponseMessage.Content.ReadAsStringAsync();
        if (httpResponseMessage.IsSuccessStatusCode)
        {
            accessToken = new TwitterGetAccessTokenResponse(response);
        }
        else
        {
            Logger.Error("Twitter API error: " + response);
            throw new UserFriendlyException("Can't get access token from twitter.");
        }

        return accessToken;
    }

    public override async Task<ExternalAuthUserInfo> GetUserInfo(string accessTokenAndSecret)
    {
        var values = accessTokenAndSecret.Split('&');
        var accessToken = values[0];
        var accessTokenSecret = values[1];
        var userClient = new TwitterClient(ProviderInfo.ClientId, ProviderInfo.ClientSecret, accessToken,
            accessTokenSecret);
        var user = await userClient.Users.GetAuthenticatedUserAsync();
        var userInfo = new ExternalAuthUserInfo
        {
            Name = user.Name,
            Surname = user.ScreenName,
            Provider = "Twitter",
            EmailAddress = user.Email,
            ProviderKey = user.IdStr
        };
        return userInfo;
    }
}