using System;
using System.Linq;

namespace Abp.AspNetZeroCore.Web.Authentication.External.Twitter;

public class TwitterResponseBase
{
    protected string GetKeyValue(string[] values, string key)
    {
        var str = values.FirstOrDefault(v => v.Contains(key));
        return string.IsNullOrEmpty(str) ? "" : str.Replace(key + "=", "");
    }

    protected bool GetKeyValueBoolean(string[] values, string key)
    {
        return GetKeyValue(values, key).Equals("true", StringComparison.InvariantCultureIgnoreCase);
    }
}