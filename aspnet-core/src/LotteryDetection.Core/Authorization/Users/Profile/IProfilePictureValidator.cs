using System.Threading.Tasks;

namespace LotteryDetection.Authorization.Users.Profile;

public interface IProfilePictureValidator
{
    Task ValidateProfilePictureDimensions(byte[] imageBytes);
    Task ValidateProfilePictureSize(byte[] imageBytes);
}