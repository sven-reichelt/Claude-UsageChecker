using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Wertet die Befehlszeile aus. Der einzige Schalter dient dem Neustart nach
/// einer Aktualisierung.
/// </summary>
internal static class StartupArguments
{
    /// <summary>
    /// So lange wird auf das Ende der Vorgaengerinstanz gewartet. Laenger zu
    /// warten hilft nicht - dann stimmt etwas anderes nicht, und die Sperre auf
    /// eine Instanz greift ohnehin als letzte Absicherung.
    /// </summary>
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Wartet nach einer Aktualisierung darauf, dass die ersetzte Fassung sich
    /// beendet - sonst scheitert diese Instanz an der Einzelinstanz-Sperre und
    /// der Nutzer stuende nach dem Update ohne laufende Anwendung da.
    /// </summary>
    public static void WaitForPredecessor(string[] args)
    {
        if (TryReadPredecessorId(args) is not { } processId)
        {
            return;
        }

        try
        {
            using var vorgaenger = Process.GetProcessById(processId);
            vorgaenger.WaitForExit((int)MaxWait.TotalMilliseconds);
        }
        catch (ArgumentException)
        {
            // Schon beendet - genau das war das Ziel.
        }
        catch (InvalidOperationException)
        {
            // Ebenso.
        }

        // Der Riegel wird erst beim Beenden freigegeben; ein kurzer Nachlauf
        // erspart ein Wettrennen darum.
        Thread.Sleep(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>Liest die Kennung der Vorgaengerinstanz aus der Befehlszeile.</summary>
    internal static int? TryReadPredecessorId(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], UpdateInstaller.WaitArgument, StringComparison.Ordinal)
                && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                && id > 0)
            {
                return id;
            }
        }

        return null;
    }
}
