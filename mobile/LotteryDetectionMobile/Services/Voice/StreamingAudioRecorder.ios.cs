#if IOS
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;

namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
/// iOS implementation of streaming audio recorder using AVAudioEngine.
/// Captures audio and resamples to 16kHz/16-bit/mono PCM for speech recognition.
/// </summary>
public class StreamingAudioRecorder : IStreamingAudioRecorder, IDisposable
{
    private AVAudioEngine? _audioEngine;
    private Channel<byte[]>? _channel;
    private bool _isRecording;
    private bool _disposed;

    // Target format for speech recognition: 16kHz, 16-bit, mono
    private const double TargetSampleRate = 16000.0;
    private const int TargetBitsPerSample = 16;
    private const int TargetChannels = 1;
    private const int ChunkDurationMs = 32;
    private const int TargetChunkSamples = (int)(TargetSampleRate * ChunkDurationMs / 1000); // 512 samples
    private const int TargetChunkBytes = TargetChunkSamples * 2; // 1024 bytes (16-bit = 2 bytes)

    // Resampling buffer (instance fields to avoid closure issues on stop/start cycles)
    private readonly byte[] _resampleBuffer = new byte[TargetChunkBytes * 2];
    private int _resampleOffset;

    // iOS Simulator detection
    private static readonly bool IsSimulator = DetectSimulator();

    private static bool DetectSimulator()
    {
#if DEBUG
        var runtimeArch = ObjCRuntime.Runtime.Arch;
        return runtimeArch == ObjCRuntime.Arch.SIMULATOR;
#else
        return false;
#endif
    }

    public bool IsRecording => _isRecording;
    public ChannelReader<byte[]> AudioChunks => _channel?.Reader ?? Channel.CreateBounded<byte[]>(1).Reader;

    // iOS: file backup is handled by IPlatformAudioRecorder (AVAudioRecorder), not here.
    public string? OutputFilePath => null;

    public event Action<double>? OnAmplitudeChanged;

    public Task StartAsync(string outputFolder, CancellationToken ct)
    {
        if (_isRecording)
        {
            Console.WriteLine("[iOS StreamingRecorder] Already recording");
            return Task.CompletedTask;
        }

        // Note: iOS Simulator on macOS 12+ can use Mac microphone
        if (IsSimulator)
        {
            Console.WriteLine("[iOS StreamingRecorder] Running on iOS Simulator - using Mac microphone");
        }

        try
        {
            // Create bounded channel (drop oldest if consumer is slow)
            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            ConfigureAudioSession();
            SetupAudioEngine();

            _audioEngine!.StartAndReturnError(out var error);
            if (error != null)
            {
                throw new InvalidOperationException($"Failed to start audio engine: {error.LocalizedDescription}");
            }

            _isRecording = true;
            Console.WriteLine("[iOS StreamingRecorder] Started recording");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[iOS StreamingRecorder] Start failed: {ex}");
            Cleanup();
            throw;
        }
    }

    private void ConfigureAudioSession()
    {
        var session = AVAudioSession.SharedInstance();

        // PlatformAudioRecorder (AVAudioRecorder) has already configured the session as
        // PlayAndRecord and activated it. Do NOT call SetCategory or SetPreferredSampleRate
        // here — changing those while AVAudioRecorder is running interrupts it and produces
        // an empty .m4a file. ConvertToTargetFormat resamples from any hardware rate, so
        // no sample-rate preference is needed.
        // Just ensure the session is active (no-op if already active).
        var activeError = session.SetActive(true);
        if (activeError != null)
            throw new InvalidOperationException($"Audio session activation error: {activeError.LocalizedDescription}");

        Console.WriteLine($"[iOS StreamingRecorder] Audio session active. Actual sample rate: {session.SampleRate}Hz");
    }

    private void SetupAudioEngine()
    {
        _audioEngine = new AVAudioEngine();
        var inputNode = _audioEngine.InputNode;
        var inputFormat = inputNode.GetBusOutputFormat(0);

        Console.WriteLine($"[iOS StreamingRecorder] Input format: {inputFormat.SampleRate}Hz, {inputFormat.ChannelCount} channels");

        // Calculate buffer size for ~32ms of audio at input sample rate
        var inputBufferSize = (uint)(inputFormat.SampleRate * ChunkDurationMs / 1000);

        // Reset resample buffer state for this recording session
        _resampleOffset = 0;
        Array.Clear(_resampleBuffer, 0, _resampleBuffer.Length);

        inputNode.InstallTapOnBus(
            0,
            inputBufferSize,
            inputFormat,
            (buffer, when) =>
            {
                if (!_isRecording || _channel == null) return;

                try
                {
                    // Convert buffer to 16kHz/16-bit/mono PCM
                    var pcmData = ConvertToTargetFormat(buffer, inputFormat.SampleRate);
                    if (pcmData.Length == 0) return;

                    // Accumulate and emit chunks of target size
                    var sourceOffset = 0;
                    while (sourceOffset < pcmData.Length)
                    {
                        var bytesToCopy = Math.Min(pcmData.Length - sourceOffset, TargetChunkBytes - _resampleOffset);
                        Array.Copy(pcmData, sourceOffset, _resampleBuffer, _resampleOffset, bytesToCopy);
                        _resampleOffset += bytesToCopy;
                        sourceOffset += bytesToCopy;

                        // Emit chunk when we have enough data
                        if (_resampleOffset >= TargetChunkBytes)
                        {
                            var chunk = new byte[TargetChunkBytes];
                            Array.Copy(_resampleBuffer, 0, chunk, 0, TargetChunkBytes);
                            _channel.Writer.TryWrite(chunk);

                            // Move remaining data to start of buffer
                            var remaining = _resampleOffset - TargetChunkBytes;
                            if (remaining > 0)
                            {
                                Array.Copy(_resampleBuffer, TargetChunkBytes, _resampleBuffer, 0, remaining);
                            }
                            _resampleOffset = remaining;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[iOS StreamingRecorder] Buffer processing error: {ex.Message}");
                }
            });
    }

    /// <summary>
    /// Converts AVAudioPCMBuffer to 16kHz/16-bit/mono PCM format.
    /// Handles resampling if input sample rate differs from target.
    /// </summary>
    private byte[] ConvertToTargetFormat(AVAudioPcmBuffer buffer, double inputSampleRate)
    {
        var frameLength = (int)buffer.FrameLength;
        if (frameLength == 0) return Array.Empty<byte>();

        // Get float data from buffer (planar format - array of channel pointers)
        var floatChannelData = buffer.FloatChannelData;
        if (floatChannelData == IntPtr.Zero) return Array.Empty<byte>();

        // Read pointer to first channel's data (float**)
        var channel0Ptr = Marshal.ReadIntPtr(floatChannelData);
        if (channel0Ptr == IntPtr.Zero) return Array.Empty<byte>();

        // Copy float samples from first channel (mono)
        var inputSamples = new float[frameLength];
        Marshal.Copy(channel0Ptr, inputSamples, 0, frameLength);

        // Compute amplitude on raw input samples (already normalized -1..1)
        OnAmplitudeChanged?.Invoke(ComputeAmplitude(inputSamples));

        // Resample if needed
        float[] outputSamples;
        if (Math.Abs(inputSampleRate - TargetSampleRate) > 1.0)
        {
            outputSamples = Resample(inputSamples, inputSampleRate, TargetSampleRate);
        }
        else
        {
            outputSamples = inputSamples;
        }

        // Convert float [-1.0, 1.0] to 16-bit PCM
        var pcmData = new byte[outputSamples.Length * 2];
        for (var i = 0; i < outputSamples.Length; i++)
        {
            var sample = Math.Clamp(outputSamples[i], -1.0f, 1.0f);
            var pcmSample = (short)(sample * 32767);
            pcmData[i * 2] = (byte)(pcmSample & 0xFF);
            pcmData[i * 2 + 1] = (byte)((pcmSample >> 8) & 0xFF);
        }

        return pcmData;
    }

    private static double ComputeAmplitude(float[] samples)
    {
        if (samples.Length == 0) return 0;
        double sumSquares = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            sumSquares += samples[i] * samples[i];
        }

        var rms = Math.Sqrt(sumSquares / samples.Length);
        // Power curve: boosts quiet/normal speech (rms ~0.02-0.08) into a visible
        // visual range while compressing loud peaks so bars don't always max out.
        var boosted = Math.Clamp(rms * 6.0, 0.0, 1.0);
        return Math.Pow(boosted, 0.6);
    }

    /// <summary>
    /// Simple linear interpolation resampling.
    /// Good enough for speech recognition; not audiophile quality.
    /// </summary>
    private static float[] Resample(float[] input, double inputRate, double outputRate)
    {
        var ratio = inputRate / outputRate;
        var outputLength = (int)(input.Length / ratio);
        var output = new float[outputLength];

        for (var i = 0; i < outputLength; i++)
        {
            var srcIndex = i * ratio;
            var srcIndexInt = (int)srcIndex;
            var frac = (float)(srcIndex - srcIndexInt);

            if (srcIndexInt + 1 < input.Length)
            {
                output[i] = input[srcIndexInt] * (1 - frac) + input[srcIndexInt + 1] * frac;
            }
            else if (srcIndexInt < input.Length)
            {
                output[i] = input[srcIndexInt];
            }
        }

        return output;
    }

    public Task StopAsync()
    {
        if (!_isRecording)
        {
            return Task.CompletedTask;
        }

        Console.WriteLine("[iOS StreamingRecorder] Stopping...");
        _isRecording = false;

        try
        {
            Cleanup();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[iOS StreamingRecorder] Stop error: {ex.Message}");
        }

        Console.WriteLine("[iOS StreamingRecorder] Stopped");
        return Task.CompletedTask;
    }

    private void Cleanup()
    {
        try
        {
            _audioEngine?.InputNode.RemoveTapOnBus(0);
            _audioEngine?.Stop();
            _audioEngine?.Dispose();
            _audioEngine = null;

            _channel?.Writer.TryComplete();

            // Do NOT call SetActive(false) here — PlatformAudioRecorder (AVAudioRecorder) may
            // still be recording in parallel and needs the session to finalize its .m4a file.
            // PlatformAudioRecorder.StopAsync() deactivates the session after it finishes.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[iOS StreamingRecorder] Cleanup error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isRecording)
        {
            _isRecording = false;
            Cleanup();
        }

        GC.SuppressFinalize(this);
    }
}
#endif