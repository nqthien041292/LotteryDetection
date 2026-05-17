namespace LotteryDetection.Configuration;

public class NullExternalLoginOptionsCacheManager : IExternalLoginOptionsCacheManager
{
    public static NullExternalLoginOptionsCacheManager Instance { get; } = new();

    public void ClearCache()
    {
    }
}