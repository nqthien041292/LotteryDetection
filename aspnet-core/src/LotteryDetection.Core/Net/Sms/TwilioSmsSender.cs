using System.Threading.Tasks;
using Abp.Dependency;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LotteryDetection.Net.Sms;

public class TwilioSmsSender : ISmsSender, ITransientDependency
{
    private readonly TwilioSmsSenderConfiguration _twilioSmsSenderConfiguration;

    public TwilioSmsSender(TwilioSmsSenderConfiguration twilioSmsSenderConfiguration)
    {
        _twilioSmsSenderConfiguration = twilioSmsSenderConfiguration;
    }

    public async Task SendAsync(string number, string message)
    {
        TwilioClient.Init(_twilioSmsSenderConfiguration.AccountSid, _twilioSmsSenderConfiguration.AuthToken);

        var resource = await MessageResource.CreateAsync(
            body: message,
            from: new PhoneNumber(_twilioSmsSenderConfiguration.SenderNumber),
            to: new PhoneNumber(number)
        );
    }
}