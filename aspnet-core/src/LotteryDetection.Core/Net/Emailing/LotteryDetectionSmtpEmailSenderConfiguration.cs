using Abp.Configuration;
using Abp.Net.Mail;
using Abp.Net.Mail.Smtp;
using Abp.Runtime.Security;

namespace LotteryDetection.Net.Emailing;

public class LotteryDetectionSmtpEmailSenderConfiguration : SmtpEmailSenderConfiguration
{
    public LotteryDetectionSmtpEmailSenderConfiguration(ISettingManager settingManager) : base(settingManager)
    {

    }

    public override string Password => SimpleStringCipher.Instance.Decrypt(GetNotEmptySettingValue(EmailSettingNames.Smtp.Password));
}

