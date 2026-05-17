namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterGetRequestTokenResponse : TwitterResponseBase
{
    public TwitterGetRequestTokenResponse(string response)
    {
        Initialize(response);
    }

    public string Token { get; set; }

    public string Secret { get; set; }

    public bool Confirmed { get; set; }

    public string RedirectUrl { get; set; }

    private void Initialize(string response)
    {
        if (string.IsNullOrEmpty(response))
            return;
        var values = response.Split('&');
        Token = GetKeyValue(values, "oauth_token");
        Secret = GetKeyValue(values, "oauth_token_secret");
        Confirmed = GetKeyValueBoolean(values, "oauth_callback_confirmed");
    }
}