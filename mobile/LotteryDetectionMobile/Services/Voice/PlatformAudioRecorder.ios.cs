#if IOS
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;

namespace LotteryDetectionMobile.Services.Voice;

public class PlatformAudioRecorder : IPlatformAudioRecorder
{
    public string? LatestFilePath { get; private set; }

    private AVAudioRecorder? recorder;

    public Task StartAsync(string folderPath, CancellationToken token)
    {
        var filename = $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss}.m4a";
        LatestFilePath = Path.Combine(folderPath, filename);

        try
        {
            var session = AVAudioSession.SharedInstance();
            var categoryError = session.SetCategory(
                AVAudioSessionCategory.PlayAndRecord,
                AVAudioSessionCategoryOptions.DefaultToSpeaker);

            if (categoryError != null)
                throw new InvalidOperationException($"Audio session category error: {categoryError.LocalizedDescription}");

            var activeError = session.SetActive(true);
            if (activeError != null)
                throw new InvalidOperationException($"Audio session activation error: {activeError.LocalizedDescription}");

            var settings = new AudioSettings
            {
                SampleRate = 44100,
                Format = AudioToolbox.AudioFormatType.MPEG4AAC,
                NumberChannels = 1,
                AudioQuality = AVAudioQuality.Medium
            };

            var url = NSUrl.FromFilename(LatestFilePath);
            var dir = Path.GetDirectoryName(LatestFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            recorder = AVAudioRecorder.Create(url, settings, out var error);
            if (recorder == null || error != null)
                throw new InvalidOperationException($"Failed to create audio recorder: {error?.LocalizedDescription ?? "unknown"}");

            if (!recorder.PrepareToRecord() || !recorder.Record())
                throw new InvalidOperationException("Audio recorder failed to start recording");

            return Task.CompletedTask;
        }
        catch (Exception)
        {
            recorder?.Dispose();
            recorder = null;
            throw;
        }
    }

    public Task StopAsync()
    {
        try
        {
            if (recorder != null)
            {
                if (recorder.Recording)
                {
                    recorder.Stop();
                }

                recorder.Dispose();
                recorder = null;
            }

            var session = AVAudioSession.SharedInstance();
            session.SetActive(false);
        }
        catch
        {
            recorder?.Dispose();
            recorder = null;
        }

        return Task.CompletedTask;
    }
}
#endif