using System.Threading.Tasks;

namespace LotteryDetection.Security.Recaptcha;

public interface IRecaptchaValidator
{
    Task ValidateAsync(string captchaResponse);
}
