namespace LotteryDetectionMobile.Services.Voice;

public interface IPlatformAudioRecorder
{
    string? LatestFilePath { get; }
    Task StartAsync(string folderPath, CancellationToken token);
    Task StopAsync();
}