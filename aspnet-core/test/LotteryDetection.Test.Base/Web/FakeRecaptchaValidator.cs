using System.Threading.Tasks;
using LotteryDetection.Security.Recaptcha;

namespace LotteryDetection.Test.Base.Web
{
    public class FakeRecaptchaValidator : IRecaptchaValidator
    {
        public Task ValidateAsync(string captchaResponse)
        {
            return Task.CompletedTask;
        }
    }
}
