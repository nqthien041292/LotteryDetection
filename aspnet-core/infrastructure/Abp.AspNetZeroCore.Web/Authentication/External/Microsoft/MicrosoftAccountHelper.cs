using System;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Microsoft;

public static class MicrosoftAccountHelper
{
    public static string GetId(JObject user)
    {
        return user != null ? user.Value<string>("id") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetDisplayName(JObject user)
    {
        return user != null ? user.Value<string>("displayName") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetGivenName(JObject user)
    {
        return user != null ? user.Value<string>("givenName") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetSurname(JObject user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        return user.ContainsKey("surname") ? user.Value<string>("surname") : string.Empty;
    }

    public static string GetEmail(JObject user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        return user.Value<string>("mail") ?? user.Value<string>("userPrincipalName");
    }
}