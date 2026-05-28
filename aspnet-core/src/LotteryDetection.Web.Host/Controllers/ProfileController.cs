using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.AspNetZeroCore.Net;
using Abp.Extensions;
using Abp.IO.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using LotteryDetection.Authorization.Users.Profile;
using LotteryDetection.Authorization.Users.Profile.Dto;
using LotteryDetection.Storage;
using Microsoft.AspNetCore.Mvc;

namespace LotteryDetection.Web.Controllers;

[AbpMvcAuthorize]
public class ProfileController : ProfileControllerBase
{
    private readonly IProfileAppService _profileAppService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ProfileController(
        ITempFileCacheManager tempFileCacheManager,
        IProfileAppService profileAppService) :
        base(tempFileCacheManager, profileAppService)
    {
        _tempFileCacheManager = tempFileCacheManager;
        _profileAppService = profileAppService;
    }

    /// <summary>
    /// Mobile-friendly single-call upload. Reads a multipart file, drops it
    /// into the ABP temp cache, then immediately commits via
    /// IProfileAppService.UpdateProfilePicture so the saved BinaryObject is
    /// linked to <c>User.ProfilePictureId</c> in one round-trip (the stock
    /// ABP flow is a two-step upload-then-update aimed at the web UI's
    /// cropper).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task UploadProfilePicture()
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            throw new UserFriendlyException("Vui lòng chọn ảnh.");

        var token = Guid.NewGuid().ToString();
        byte[] bytes;
        await using (var stream = file.OpenReadStream())
        {
            bytes = stream.GetAllBytes();
        }
        _tempFileCacheManager.SetFile(token, bytes);

        await _profileAppService.UpdateProfilePicture(new UpdateProfilePictureInput
        {
            FileToken = token,
            UseGravatarProfilePicture = false
        });
    }

    /// <summary>
    /// Returns the signed-in user's current profile picture as a JPEG stream.
    /// Falls back to the default placeholder when no picture is set.
    /// </summary>
    [HttpGet]
    public async Task<FileResult> GetMyProfilePicture()
    {
        var userId = AbpSession.GetUserId();
        var output = await _profileAppService.GetProfilePictureByUser(userId);
        if (output.ProfilePicture.IsNullOrEmpty())
            return GetDefaultProfilePictureInternal();
        return File(Convert.FromBase64String(output.ProfilePicture), MimeTypeNames.ImageJpeg);
    }
}
