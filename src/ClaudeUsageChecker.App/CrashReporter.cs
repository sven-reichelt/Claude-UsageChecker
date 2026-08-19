using System;
using System.IO;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Schreibt Ausnahmen in eine lokale Datei. Der Bericht bleibt ausschliesslich
/// auf dem Geraet und wird nirgendwohin uebertragen.
/// </summary>
internal static class CrashReporter
{
    private static readonly object WriteLock = new();

    /// <summary>Pfad der Protokolldatei im lokalen Profil.</summary>
    public static string LogFile { get; } = Path.Combine(AppPaths.LocalDataDirectory, "crash.log");

    /// <summary>Hinterlegt Ausnahmen, die ausserhalb jedes Handlers auftreten.</summary>
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
            // Ein nicht abgewarteter Fehler im Hintergrund darf den Prozess nicht beenden.
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
            // Beim Schreiben des Fehlerberichts darf nichts mehr schiefgehen.
        }
    }
}
