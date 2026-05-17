using System.Threading.Tasks;
using Abp.Dependency;
using Abp.UI;
using LotteryDetection.Authorization.Users;
using Microsoft.AspNetCore.Identity;

namespace LotteryDetection.Authentication.TwoFactor.Google;

public class GoogleAuthenticatorProvider : LotteryDetectionServiceBase, IUserTwoFactorTokenProvider<User>,
    ITransientDependency
{
    public const string Name = "GoogleAuthenticator";
    private readonly GoogleTwoFactorAuthenticateService _googleTwoFactorAuthenticateService;

    public GoogleAuthenticatorProvider(GoogleTwoFactorAuthenticateService googleTwoFactorAuthenticateService)
    {
        _googleTwoFactorAuthenticateService = googleTwoFactorAuthenticateService;
    }

    public Task<string> GenerateAsync(string purpose, UserManager<User> userManager, User user)
    {
        CheckIfGoogleAuthenticatorIsEnabled(user);

        var setupInfo = _googleTwoFactorAuthenticateService.GenerateSetupCode("LotteryDetection", user.EmailAddress,
            user.GoogleAuthenticatorKey, 300, 300);

        return Task.FromResult(setupInfo.QrCodeSetupImageUrl);
    }

    public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> userManager, User user)
    {
        CheckIfGoogleAuthenticatorIsEnabled(user);

        return Task.FromResult(
            _googleTwoFactorAuthenticateService.ValidateTwoFactorPin(user.GoogleAuthenticatorKey, token));
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> userManager, User user)
    {
        return Task.FromResult(user.IsTwoFactorEnabled && user.GoogleAuthenticatorKey != null);
    }

    private void CheckIfGoogleAuthenticatorIsEnabled(User user)
    {
        if (user.GoogleAuthenticatorKey == null) throw new UserFriendlyException(L("GoogleAuthenticatorIsNotEnabled"));
    }
}