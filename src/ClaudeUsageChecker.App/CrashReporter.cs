using System;
using System.IO;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Writes exceptions to a local file. The report stays on the machine and is
/// transmitted nowhere.
/// </summary>
internal static class CrashReporter
{
    private static readonly object WriteLock = new();

    /// <summary>Path of the log file inside the local profile.</summary>
    public static string LogFile { get; } = Path.Combine(AppPaths.LocalDataDirectory, "crash.log");

    /// <summary>Records exceptions that occur outside any handler.</summary>
    public static void InstallGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                Write(exception, "AppDomain.UnhandledException");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Write(e.Exception, "TaskScheduler.UnobservedTaskException");
            // An unobserved background failure must not end the process.
            e.SetObserved();
        };
    }

    public static void Write(Exception exception, string? context = null)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LocalDataDirectory);

            var header = context is null
                ? $"[{DateTimeOffset.Now:o}]"
                : $"[{DateTimeOffset.Now:o}] ({context})";

            lock (WriteLock)
            {
                File.AppendAllText(
                    LogFile,
                    $"{header} {exception}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // Nothing may go wrong while writing the crash report itself.
        }
    }
}
