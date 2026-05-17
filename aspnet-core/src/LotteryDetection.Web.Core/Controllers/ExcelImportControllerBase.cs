using System;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.IO.Extensions;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using Abp.Web;
using Abp.Web.Models;
using LotteryDetection.DataImporting.Excel;
using LotteryDetection.Storage;
using Microsoft.AspNetCore.Mvc;

namespace LotteryDetection.Web.Controllers;

public abstract class ExcelImportControllerBase(
    IBinaryObjectManager binaryObjectManager,
    IBackgroundJobManager backgroundJobManager)
    : LotteryDetectionControllerBase
{
    protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
    protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

    public abstract string ImportExcelPermission { get; }

    [HttpPost]
    public async Task<JsonResult> ImportFromExcel()
    {
        if (!await PermissionChecker.IsGrantedAsync(ImportExcelPermission))
            throw new AbpAuthorizationException(
                LocalizationManager.GetString(AbpWebConsts.LocalizationSourceName,
                    "DefaultError403")
            );

        try
        {
            var file = Request.Form.Files[0];

            if (file == null) throw new UserFriendlyException(L("File_Empty_Error"));

            if (file.Length > 1048576 * 100) //100 MB
                throw new UserFriendlyException(L("File_SizeLimit_Error"));

            byte[] fileBytes;

            await using (var stream = file.OpenReadStream())
            {
                fileBytes = await stream.GetAllBytesAsync();
            }

            var tenantId = AbpSession.TenantId;
            var fileObject = new BinaryObject(tenantId, fileBytes, $"{DateTime.UtcNow} import from excel file.");

            await BinaryObjectManager.SaveAsync(fileObject);

            var args = new ImportFromExcelJobArgs
            {
                TenantId = tenantId,
                BinaryObjectId = fileObject.Id,
                ExcelImporter = AbpSession.ToUserIdentifier()
            };

            await EnqueueExcelImportJobAsync(args);

            return Json(new AjaxResponse(new { }));
        }
        catch (UserFriendlyException ex)
        {
            return Json(new AjaxResponse(new ErrorInfo(ex.Message)));
        }
    }

    public abstract Task EnqueueExcelImportJobAsync(ImportFromExcelJobArgs args);
}