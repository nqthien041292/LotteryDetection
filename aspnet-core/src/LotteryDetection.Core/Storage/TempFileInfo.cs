namespace LotteryDetection.Storage;

public class TempFileInfo
{
    public TempFileInfo()
    {
    }

    public TempFileInfo(byte[] file)
    {
        File = file;
    }

    public TempFileInfo(string fileName, string fileType, byte[] file)
    {
        FileName = fileName;
        FileType = fileType;
        File = file;
    }

    public string FileName { get; set; }
    public string FileType { get; set; }
    public byte[] File { get; set; }
}