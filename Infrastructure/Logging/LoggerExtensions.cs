using System.IO;
using Serilog;
using Serilog.Events;

namespace ImageAIRenamer.Infrastructure.Logging;

/// <summary>
/// Extension methods for configuring Serilog logging
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Configures Serilog logger for the application
    /// </summary>
    /// <returns>Configured ILogger instance</returns>
    public static ILogger ConfigureLogger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logFolder = Path.Combine(appData, "ImageAIRenamer", "logs");
        Directory.CreateDirectory(logFolder);

        var logFilePath = Path.Combine(logFolder, "app-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
