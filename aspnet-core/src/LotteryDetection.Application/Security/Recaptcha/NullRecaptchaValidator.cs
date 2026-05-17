using System.Threading.Tasks;

namespace LotteryDetection.Security.Recaptcha;

public class NullRecaptchaValidator : IRecaptchaValidator
{
    public static NullRecaptchaValidator Instance { get; } = new();

    public Task ValidateAsync(string captchaResponse)
    {
        return Task.CompletedTask;
    }
}