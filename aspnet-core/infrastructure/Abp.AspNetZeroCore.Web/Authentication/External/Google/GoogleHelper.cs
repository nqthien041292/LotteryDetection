using System;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Google;

public static class GoogleHelper
{
    public static string GetId(JObject user)
    {
        return user != null ? user.Value<string>("id") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetName(JObject user)
    {
        return user != null ? user.Value<string>("name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetGivenName(JObject user)
    {
        return user != null ? user.Value<string>("given_name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetFamilyName(JObject user)
    {
        return user != null ? user.Value<string>("family_name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetProfile(JObject user)
    {
        return user != null ? user.Value<string>("link") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetEmail(JObject user)
    {
        return user != null ? user.Value<string>("email") : throw new ArgumentNullException(nameof(user));
    }

    private static string TryGetValue(JObject user, string propertyName, string subProperty)
    {
        if (!user.TryGetValue(propertyName, out var jToken)) return null;
        var jObject = JObject.Parse(jToken.ToString());
        return jObject.TryGetValue(subProperty, out jToken) ? jToken.ToString() : null;
    }

    private static string TryGetFirstValue(JObject user, string propertyName, string subProperty)
    {
        if (!user.TryGetValue(propertyName, out var jToken)) return null;
        var jArray = JArray.Parse(jToken.ToString());
        if (jArray is not { Count: > 0 }) return null;
        var jObject = JObject.Parse(jArray.First?.ToString() ?? throw new InvalidOperationException());
        return jObject.TryGetValue(subProperty, out jToken) ? jToken.ToString() : null;
    }
}