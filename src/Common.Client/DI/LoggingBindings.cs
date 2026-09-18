using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace Common.Client.DI;

/// <summary>
/// Registers application logging.
/// </summary>
public static class LoggingBindings
{
    /// <summary>
    /// Registers debug logging and, outside the designer, file logging.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <param name="isDesigner">Whether the app is running in the Avalonia designer.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithLogging(this IServiceCollection container, bool isDesigner)
    {
        _ = container.AddLogging(builder =>
        {
            _ = builder.AddDebug();

            if (!isDesigner)
            {
                _ = builder.AddFile(
                    ClientProperties.PathToLogFile,
                    opt =>
                    {
                        opt.Append = false;
                        opt.FormatLogFileName = fileName => string.Format(fileName, DateTime.UtcNow);
                        opt.FormatLogEntry = message =>
                            $"[{DateTime.Now.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}] {message.LogLevel,-11} {message.Message} {message.Exception}";
                    });

                _ = builder.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
                _ = builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
            }
        });

        _ = container.AddSingleton<ILogger>(serviceProvider =>
            serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Superheater"));

        return container;
    }
}
