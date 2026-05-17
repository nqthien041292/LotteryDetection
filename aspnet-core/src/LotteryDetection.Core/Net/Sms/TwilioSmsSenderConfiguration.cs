using Abp.Dependency;
using LotteryDetection.Configuration;
using Microsoft.Extensions.Configuration;

namespace LotteryDetection.Net.Sms;

public class TwilioSmsSenderConfiguration : ITransientDependency
{
    private readonly IConfigurationRoot _appConfiguration;

    public TwilioSmsSenderConfiguration(IAppConfigurationAccessor configurationAccessor)
    {
        _appConfiguration = configurationAccessor.Configuration;
    }

    public string AccountSid => _appConfiguration["Twilio:AccountSid"];

    public string AuthToken => _appConfiguration["Twilio:AuthToken"];

    public string SenderNumber => _appConfiguration["Twilio:SenderNumber"];
}