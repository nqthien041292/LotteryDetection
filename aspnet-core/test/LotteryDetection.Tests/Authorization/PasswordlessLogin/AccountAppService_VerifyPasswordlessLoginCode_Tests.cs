using Abp.Runtime.Caching;
using LotteryDetection.Authorization.Accounts;
using LotteryDetection.Authorization.PasswordlessLogin;
using LotteryDetection.Authorization.Accounts.Dto;
using System.Threading.Tasks;
using Abp.UI;
using Shouldly;
using Xunit;

namespace LotteryDetection.Tests.Authorization.PasswordlessLogin
{
    public class AccountAppService_VerifyPasswordlessLoginCode_Tests : PasswordlessLognTestBase
    {
        private readonly IAccountAppService _accountAppService;
        private readonly ICacheManager _cacheManager;

        public AccountAppService_VerifyPasswordlessLoginCode_Tests()
        {
            _accountAppService = Resolve<IAccountAppService>();
            _cacheManager = Resolve<ICacheManager>();

            SetMockCode();
        }

        [Fact]
        public async Task Verify_Valid_Passwordless_Login_Code()
        {
            await CreateAndSetUser();

            var cacheItem = new PasswordlessLoginCodeCacheItem
            {
                Code = MockCode
            };

            var input = new VerifyPasswordlessLoginCodeInput
            {
                ProviderValue = ProviderKeyEmail,
                Code = MockCode
            };

            await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
                GetPasswordlessLoginCodeCacheKey(AbpSession.TenantId, ProviderKeyEmail),
                cacheItem
            );

            await _accountAppService.VerifyPasswordlessLoginCode(input);
        }

        [Fact]
        public async Task Verify_Invalid_Passwordless_Login_Code()
        {
            await CreateAndSetUser();

            var cacheItem = new PasswordlessLoginCodeCacheItem
            {
                Code = MockCode
            };

            var input = new VerifyPasswordlessLoginCodeInput
            {
                ProviderValue = ProviderKeyEmail,
                Code = "123456"
            };

            await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
                GetPasswordlessLoginCodeCacheKey(AbpSession.TenantId, ProviderKeyEmail),
                cacheItem
            );

            var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            {
                await _accountAppService.VerifyPasswordlessLoginCode(input);
            });
            exception.Message.ShouldBe("Wrong verification code!");
        }
    }
}