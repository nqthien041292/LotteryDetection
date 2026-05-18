#if ANDROID
using Android.Media;

namespace LotteryDetectionMobile.Services.Voice;

public class PlatformAudioRecorder : IPlatformAudioRecorder
{
    private bool prepared;

    private MediaRecorder? recorder;
    public string? LatestFilePath { get; private set; }

    public Task StartAsync(string folderPath, CancellationToken token)
    {
        var filename = $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss}.m4a";
        LatestFilePath = Path.Combine(folderPath, filename);

        recorder ??= new MediaRecorder();
        recorder.Reset();

        var dir = Path.GetDirectoryName(LatestFilePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        recorder.SetAudioSource(AudioSource.Mic);
        recorder.SetOutputFormat(OutputFormat.Mpeg4);
        recorder.SetAudioEncoder(AudioEncoder.Aac);
        recorder.SetAudioEncodingBitRate(128000);
        recorder.SetAudioSamplingRate(44100);
        recorder.SetOutputFile(LatestFilePath);

        try
        {
            recorder.Prepare();
            prepared = true;
            recorder.Start();
        }
        catch (Exception ex)
        {
            prepared = false;
            Console.WriteLine($"[Android MediaRecorder] Prepare/Start failed: {ex}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (recorder != null && prepared)
            try
            {
                recorder.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Android MediaRecorder] Stop failed: {ex.Message}");
            }

        recorder?.Release();
        recorder = null;
        prepared = false;

        if (!string.IsNullOrEmpty(LatestFilePath) && File.Exists(LatestFilePath))
        {
            var size = new FileInfo(LatestFilePath).Length;
            Console.WriteLine($"[Android MediaRecorder] Stopped, file size: {size} bytes ({LatestFilePath})");
        }

        return Task.CompletedTask;
    }
}
#endif