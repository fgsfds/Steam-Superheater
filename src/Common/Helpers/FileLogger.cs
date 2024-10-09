using Microsoft.Extensions.Logging;
using System.Text;

namespace Common.Helpers;

public sealed class FileLogger : ILogger
{
    private readonly FileStream _logFileWriter;

    public FileLogger(FileStream logFileWriter)
    {
        _logFileWriter = logFileWriter;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            var message = formatter(state, exception);
            var line = $"[{logLevel}] {message}{Environment.NewLine}";

            _logFileWriter.Write(Encoding.ASCII.GetBytes(line));

            if (exception is not null)
            {
                _logFileWriter.Write(Encoding.ASCII.GetBytes(exception.ToString() + Environment.NewLine));
            }
            _logFileWriter.Flush();
        }
        catch
        {
            //nothing to do
        }
    }
}

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileStream _logFileWriter;

    public FileLoggerProvider(FileStream logFileWriter)
    {
        _logFileWriter = logFileWriter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_logFileWriter);
    }

    public void Dispose()
    {
        _logFileWriter.Dispose();
    }
}

public static class FileLoggerFactory
{
    private static FileStream? _logFileWriter;

    public static ILogger Create(string logFilePath)
    {
        try
        {
            _logFileWriter = File.Open(logFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        }
        catch
        {
            //nothing to do
        }

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            if (_logFileWriter is not null)
            {
                _ = builder.AddProvider(new FileLoggerProvider(_logFileWriter));
            }

            _ = builder.AddDebug();
        });

        var logger = loggerFactory.CreateLogger(string.Empty);

        return logger;
    }
}
