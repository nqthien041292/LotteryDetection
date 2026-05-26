using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using LotteryDetection.Authorization.Users.Dto;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.Authorization.Users;

[AbpAuthorize]
public class DeviceTokenAppService : LotteryDetectionAppServiceBase, IDeviceTokenAppService
{
    private readonly IRepository<UserDeviceToken, long> _deviceTokenRepository;

    public DeviceTokenAppService(IRepository<UserDeviceToken, long> deviceTokenRepository)
    {
        _deviceTokenRepository = deviceTokenRepository;
    }

    public async Task RegisterDeviceToken(RegisterDeviceTokenInput input)
    {
        var userId = AbpSession.UserId.Value;

        // Check if token already exists for this user
        var existingToken = await _deviceTokenRepository.GetAll()
            .Where(t => t.UserId == userId && t.Token == input.Token)
            .FirstOrDefaultAsync();

        if (existingToken != null)
        {
            existingToken.DeviceType = input.DeviceType;
            existingToken.DeviceName = input.DeviceName;
            await _deviceTokenRepository.UpdateAsync(existingToken);
        }
        else
        {
            // Optional: Remove this token if it belongs to another user (device was handed over)
            var tokenOnOtherUser = await _deviceTokenRepository.GetAll()
                .Where(t => t.Token == input.Token)
                .ToListAsync();
            
            foreach (var t in tokenOnOtherUser)
            {
                await _deviceTokenRepository.DeleteAsync(t);
            }

            await _deviceTokenRepository.InsertAsync(new UserDeviceToken
            {
                UserId = userId,
                Token = input.Token,
                DeviceType = input.DeviceType,
                DeviceName = input.DeviceName
            });
        }
    }
}
