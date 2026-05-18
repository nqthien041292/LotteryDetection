namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
///     Writes a valid 16kHz/16-bit/mono WAV file from streamed PCM chunks.
///     Header sizes are patched on Dispose with the actual byte count.
/// </summary>
public sealed class PcmWavWriter : IDisposable
{
    private const int SampleRate = 16000;
    private const short BitsPerSample = 16;
    private const short Channels = 1;
    private const int HeaderSize = 44;

    private readonly FileStream _stream;
    // Serializes Write vs Dispose. The PCM read loop on Android can call Write
    // concurrently with Dispose if the recorder is torn down without awaiting
    // the read task (e.g. DI container disposing during recording, or Stop's
    // 2s read-task timeout firing). Without this, FileStream writes interleave
    // with header-patching seeks and produce a corrupted WAV.
    private readonly object _ioLock = new();
    private int _dataBytesWritten;
    private bool _disposed;

    public PcmWavWriter(string filePath)
    {
        FilePath = filePath;
        _stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        WritePlaceholderHeader();
    }

    public string FilePath { get; }

    public void Write(byte[] pcm, int byteCount)
    {
        if (byteCount <= 0) return;
        lock (_ioLock)
        {
            if (_disposed) return;
            _stream.Write(pcm, 0, byteCount);
            _dataBytesWritten += byteCount;
        }
    }

    public void Dispose()
    {
        lock (_ioLock)
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                PatchHeaderSizes();
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PcmWavWriter] Dispose error: {ex.Message}");
            }
            finally
            {
                _stream.Dispose();
            }
        }
    }

    private void WritePlaceholderHeader()
    {
        const int byteRate = SampleRate * Channels * BitsPerSample / 8;
        const short blockAlign = (short)(Channels * BitsPerSample / 8);

        using var writer = new BinaryWriter(_stream, System.Text.Encoding.ASCII, true);
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(0); // RIFF size — patched later
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // PCM format
        writer.Write(Channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(BitsPerSample);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(0); // data size — patched later
    }

    private void PatchHeaderSizes()
    {
        var riffSize = 36 + _dataBytesWritten;
        _stream.Seek(4, SeekOrigin.Begin);
        _stream.Write(BitConverter.GetBytes(riffSize), 0, 4);

        _stream.Seek(40, SeekOrigin.Begin);
        _stream.Write(BitConverter.GetBytes(_dataBytesWritten), 0, 4);
    }
}
