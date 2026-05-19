using System.Collections.Concurrent;
using System.Net.Http.Json;
using LotteryDetection.Mobile.Services.Configuration;

namespace LotteryDetection.Mobile.Services.Logging;

/// <summary>
///     Local logging service. Backend logging is disabled while mobile uses mock data.
/// </summary>
public class RemoteLogService : IDisposable
{
    private const int MaxBatchSize = 50;
    private const int FlushIntervalMs = 5000; // 5 seconds
    private static RemoteLogService _instance;
    private static readonly object _lock = new();
    private readonly string _deviceId;
    private readonly Timer _flushTimer;

    private readonly HttpClient _httpClient;
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly string _platform;
    private bool _disposed;

    private RemoteLogService()
    {
        _httpClient = new HttpClient();

        _deviceId = $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}-{DeviceInfo.Platform}";
        _platform = DeviceInfo.Platform.ToString();

        // Start periodic flush
        _flushTimer = new Timer(async _ => await FlushAsync(), null, FlushIntervalMs, FlushIntervalMs);

        Log("RemoteLog", "info", $"Local mock logging initialized. Device: {_deviceId}");
    }

    public static RemoteLogService Instance
    {
        get
        {
            if (_instance == null)
                lock (_lock)
                {
                    _instance ??= new RemoteLogService();
                }

            return _instance;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer?.Dispose();

        // Final flush
        FlushAsync().GetAwaiter().GetResult();

        _httpClient?.Dispose();
    }

    /// <summary>
    ///     Log a message with category and level.
    /// </summary>
    public void Log(string category, string level, string message, Dictionary<string, string> extra = null)
    {
        var entry = new LogEntry
        {
            Level = level,
            Category = category,
            Message = message,
            Timestamp = DateTime.UtcNow,
            Extra = extra
        };

        _logQueue.Enqueue(entry);

        // Also write to console for local debugging
        Console.WriteLine($"[{category}] [{level.ToUpper()}] {message}");

        // Flush immediately if queue is getting large
        if (_logQueue.Count >= MaxBatchSize) _ = FlushAsync();
    }

    /// <summary>
    ///     Log info level message.
    /// </summary>
    public void Info(string category, string message, Dictionary<string, string> extra = null)
    {
        Log(category, "info", message, extra);
    }

    /// <summary>
    ///     Log warning level message.
    /// </summary>
    public void Warn(string category, string message, Dictionary<string, string> extra = null)
    {
        Log(category, "warn", message, extra);
    }

    /// <summary>
    ///     Log error level message.
    /// </summary>
    public void Error(string category, string message, Exception ex = null, Dictionary<string, string> extra = null)
    {
        extra ??= new Dictionary<string, string>();
        if (ex != null)
        {
            extra["exception"] = ex.GetType().Name;
            extra["exceptionMessage"] = ex.Message;
            extra["stackTrace"] = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0));
        }

        Log(category, "error", message, extra);
    }

    /// <summary>
    ///     Log debug level message.
    /// </summary>
    public void Debug(string category, string message, Dictionary<string, string> extra = null)
    {
        Log(category, "debug", message, extra);
    }

    /// <summary>
    ///     Clears pending logs locally. Backend log upload is disabled until API support exists.
    /// </summary>
    public async Task FlushAsync()
    {
        if (_logQueue.IsEmpty) return;

        var logs = new List<LogEntry>();
        while (_logQueue.TryDequeue(out var entry) && logs.Count < MaxBatchSize) logs.Add(entry);

        if (logs.Count == 0) return;

        await Task.CompletedTask;
    }

    private class LogEntry
    {
        public string Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public DateTime? Timestamp { get; set; }
        public Dictionary<string, string> Extra { get; set; }
    }

    private class LogBatch
    {
        public string DeviceId { get; set; }
        public string AppVersion { get; set; }
        public string Platform { get; set; }
        public List<LogEntry> Logs { get; set; }
    }
}
