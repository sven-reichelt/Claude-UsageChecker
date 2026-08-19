using System;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Kapselt Aktionen aus der Oberflaeche, damit ein Fehler darin die Anwendung
/// nicht beendet.
/// </summary>
/// <remarks>
/// Eine Anwendung im Infobereich hat kein Fenster, in dem ein Fehler auffallen
/// wuerde: Eine Ausnahme in einem Menue-Handler laeuft bis in die Nachrichtenschleife
/// durch und beendet den Prozess kommentarlos. Der Nutzer sieht dann nur, dass das
/// Symbol verschwunden ist. Deshalb faengt dieser Wachposten jede Ausnahme ab,
/// protokolliert sie mit Kontext und laesst die Anwendung weiterlaufen.
/// </remarks>
internal static class ErrorGuard
{
    /// <summary>Fuehrt eine Aktion aus und faengt jeden Fehler ab.</summary>
    public static void Run(string context, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex, context);
        }
    }

    /// <summary>
    /// Startet eine asynchrone Aktion, ohne auf sie zu warten, und faengt jeden
    /// Fehler ab. Ersetzt das blosse Verwerfen der Aufgabe per Unterstrich.
    /// </summary>
    public static void Forget(string context, Func<Task> action)
    {
        _ = RunAsync(context, action);
    }

    /// <summary>Fuehrt eine asynchrone Aktion aus und faengt jeden Fehler ab.</summary>
    public static async Task RunAsync(string context, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Beim Beenden erwartet.
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex, context);
        }
    }
}
