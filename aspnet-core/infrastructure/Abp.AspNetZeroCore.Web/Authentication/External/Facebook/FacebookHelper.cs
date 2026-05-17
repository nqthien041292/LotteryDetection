using System;
using Newtonsoft.Json.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Facebook;

public static class FacebookHelper
{
    public static string GetId(JObject user)
    {
        return user != null ? user.Value<string>("id") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetAgeRangeMin(JObject user)
    {
        return user != null ? TryGetValue(user, "age_range", "min") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetAgeRangeMax(JObject user)
    {
        return user != null ? TryGetValue(user, "age_range", "max") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetBirthday(JObject user)
    {
        return user != null ? user.Value<string>("birthday") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetEmail(JObject user)
    {
        return user != null ? user.Value<string>("email") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetFirstName(JObject user)
    {
        return user != null ? user.Value<string>("first_name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetGender(JObject user)
    {
        return user != null ? user.Value<string>("gender") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetLastName(JObject user)
    {
        return user != null ? user.Value<string>("last_name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetLink(JObject user)
    {
        return user != null ? user.Value<string>("link") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetLocation(JObject user)
    {
        return user != null ? TryGetValue(user, "location", "name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetLocale(JObject user)
    {
        return user != null ? user.Value<string>("locale") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetMiddleName(JObject user)
    {
        return user != null ? user.Value<string>("middle_name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetName(JObject user)
    {
        return user != null ? user.Value<string>("name") : throw new ArgumentNullException(nameof(user));
    }

    public static string GetTimeZone(JObject user)
    {
        return user != null ? user.Value<string>("timezone") : throw new ArgumentNullException(nameof(user));
    }

    private static string TryGetValue(JObject user, string propertyName, string subProperty)
    {
        if (!user.TryGetValue(propertyName, out var jToken)) return null;
        var jObject = JObject.Parse(jToken.ToString());
        return jObject.TryGetValue(subProperty, out jToken) ? jToken.ToString() : null;
    }
}