namespace Abp.AspNetZeroCore.Web.Authentication.External;

public class ClaimKeyValue
{
    public ClaimKeyValue(string type, string value)
    {
        Type = type;
        Value = value;
    }

    public string Type { get; set; }

    public string Value { get; set; }
}