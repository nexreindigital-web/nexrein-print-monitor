using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PrintMonitor.Utilities;

public class PrintMonitorFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly LogLevel _minLevel;
    private static readonly object FileLock = new();

    private static readonly Regex TokenRedactionRegex = new(
        @"(api_key|apikey|token|password|secret|bearer)\s*[:=]\s*[""']?([^""'\s]+)[""']?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PrintMonitorFileLogger(string categoryName, string logDirectory, LogLevel minLevel)
    {
        _categoryName = categoryName;
        _logDirectory = logDirectory;
        _minLevel = minLevel;

        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        catch
        {
            // Ignore directory creation failures here; handled in write
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        // Redact any sensitive tokens/secrets
        message = RedactSecrets(message);

        var logDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var logTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "INF"
        };

        var logLine = $"[{logTime} UTC] [{levelStr}] [{_categoryName}] {message}";
        if (exception != null)
        {
            logLine += Environment.NewLine + exception.ToString();
        }

        WriteToFile(logDate, logLine);

        // If Error or Critical, attempt to write to Windows Event Log if on Windows
        if (logLevel >= LogLevel.Error)
        {
            TryWriteToEventLog(logLevel, message, exception);
        }
    }

    private void WriteToFile(string dateStr, string line)
    {
        try
        {
            lock (FileLock)
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                var filePath = Path.Combine(_logDirectory, $"printmonitor-{dateStr}.log");
                File.AppendAllText(filePath, line + Environment.NewLine);

                // Run periodic cleanup (retention) once in a while
                CleanOldLogs();
            }
        }
        catch
        {
            // Do not crash the application on log failure
        }
    }

    private static DateTime _lastCleanup = DateTime.MinValue;
    private void CleanOldLogs()
    {
        if ((DateTime.UtcNow - _lastCleanup).TotalHours < 6)
            return;

        _lastCleanup = DateTime.UtcNow;
        try
        {
            var directoryInfo = new DirectoryInfo(_logDirectory);
            var files = directoryInfo.GetFiles("printmonitor-*.log");
            var threshold = DateTime.UtcNow.AddDays(-14);

            foreach (var file in files)
            {
                if (file.LastWriteTimeUtc < threshold)
                {
                    file.Delete();
                }
            }
        }
        catch
        {
            // Ignore retention cleanup errors
        }
    }

    public static string RedactSecrets(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return TokenRedactionRegex.Replace(input, "$1: [REDACTED]");
    }

    private static void TryWriteToEventLog(LogLevel level, string message, Exception? ex)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            const string source = "PrintMonitor";

            if (!EventLog.SourceExists(source))
            {
                // Creating an event source usually requires administrative privileges during installer execution.
                // If it doesn't exist, we skip creation dynamically to avoid permission exceptions.
                return;
            }

            var entryType = level == LogLevel.Critical ? EventLogEntryType.Error : EventLogEntryType.Warning;
            var fullMsg = message + (ex != null ? Environment.NewLine + ex.Message : "");
            EventLog.WriteEntry(source, fullMsg, entryType);
        }
        catch
        {
            // Best effort only
        }
    }
}

public class PrintMonitorLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly LogLevel _minLevel;

    public PrintMonitorLoggerProvider(string logDirectory, LogLevel minLevel)
    {
        _logDirectory = logDirectory;
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new PrintMonitorFileLogger(categoryName, _logDirectory, _minLevel);
    }

    public void Dispose()
    {
    }
}
