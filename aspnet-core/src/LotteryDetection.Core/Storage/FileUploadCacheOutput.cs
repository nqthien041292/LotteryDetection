using Abp.Web.Models;

namespace LotteryDetection.Storage;

public class FileUploadCacheOutput : ErrorInfo
{
    public FileUploadCacheOutput(string fileToken)
    {
        FileToken = fileToken;
    }

    public FileUploadCacheOutput(ErrorInfo error)
    {
        Code = error.Code;
        Details = error.Details;
        Message = error.Message;
        ValidationErrors = error.ValidationErrors;
    }

    public string FileToken { get; set; }
}