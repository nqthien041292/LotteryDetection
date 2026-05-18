using System.Threading.Channels;

namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
///     Interface for streaming audio recorder that provides real-time PCM audio chunks.
///     Implementations capture audio at 16kHz/16-bit/mono format suitable for speech recognition.
/// </summary>
public interface IStreamingAudioRecorder
{
    /// <summary>
    ///     Whether audio capture is currently active.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    ///     Channel reader for consuming PCM audio chunks.
    ///     Each chunk is raw 16-bit PCM at 16kHz mono (~32ms per chunk = 1024 bytes).
    /// </summary>
    ChannelReader<byte[]> AudioChunks { get; }

    /// <summary>
    ///     Fired for each captured chunk with normalized RMS amplitude (0..1).
    ///     Invoked from a background thread.
    /// </summary>
    event Action<double>? OnAmplitudeChanged;

    /// <summary>
    ///     Path to the audio file written alongside chunk emission, or null if not applicable.
    ///     Android: WAV reconstructed from PCM chunks (single mic source).
    ///     iOS: null — file recording is handled separately by IPlatformAudioRecorder.
    /// </summary>
    string? OutputFilePath { get; }

    /// <summary>
    ///     Starts audio capture and begins emitting chunks to AudioChunks channel.
    ///     On Android, also writes a WAV file at <paramref name="outputFolder"/>.
    /// </summary>
    Task StartAsync(string outputFolder, CancellationToken ct);

    /// <summary>
    ///     Stops audio capture and completes the AudioChunks channel.
    /// </summary>
    Task StopAsync();
}