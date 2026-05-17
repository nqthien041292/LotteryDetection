namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterGetAccessTokenResponse : TwitterResponseBase
{
    public TwitterGetAccessTokenResponse(string response)
    {
        Initialize(response);
    }

    public string AccessToken { get; set; }

    public string AccessTokenSecret { get; set; }

    public string UserId { get; set; }

    public string UserName { get; set; }

    private void Initialize(string response)
    {
        if (string.IsNullOrEmpty(response))
            return;
        var values = response.Split('&');
        AccessToken = GetKeyValue(values, "oauth_token");
        AccessTokenSecret = GetKeyValue(values, "oauth_token_secret");
        UserId = GetKeyValue(values, "user_id");
        UserName = GetKeyValue(values, "screen_name");
    }
}