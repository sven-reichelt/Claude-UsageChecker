using System;
using System.IO;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Schreibt unbehandelte Ausnahmen in eine lokale Datei. Der Bericht bleibt
/// ausschliesslich auf dem Geraet und wird nirgendwohin uebertragen.
/// </summary>
internal static class CrashReporter
{
    public static void Write(Exception exception)
    {
        try
        {
            var path = Path.Combine(AppPaths.LocalDataDirectory, "crash.log");
            Directory.CreateDirectory(AppPaths.LocalDataDirectory);
            File.AppendAllText(path, $"[{DateTimeOffset.Now:o}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // Beim Absturzbericht darf nichts mehr schiefgehen.
        }
    }
}
