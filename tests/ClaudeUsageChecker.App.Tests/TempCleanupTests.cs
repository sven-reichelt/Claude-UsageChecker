using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Prueft die Zurueckhaltung des Aufraeumens.
/// </summary>
/// <remarks>
/// Ein Aufraeumer, der zu viel loescht, ist schlimmer als gar keiner - er wuerde
/// die Entpackung einer laufenden Fassung entfernen. Deshalb wird im
/// Entwicklungsstand, wo sich der benutzte Ordner nicht bestimmen laesst,
/// bewusst nichts angefasst.
/// </remarks>
public class TempCleanupTests
{
    [Fact]
    public void OhneErkennbarenEigenenOrdnerWirdNichtsGeloescht()
    {
        // Im Entwicklungsstand liegt keine Entpackung vor, es laedt also kein
        // Modul von dort. Damit fehlt jede Grundlage zu entscheiden, welcher
        // Ordner in Benutzung ist - und dann wird bewusst nichts angefasst.
        Assert.Equal(0, TempCleanup.RaeumeAlteEntpackungenWeg());
    }

    [Fact]
    public void DasAufraeumenLaesstDasTemporaerverzeichnisUnberuehrt()
    {
        var basis = Path.Combine(Path.GetTempPath(), ".net");
        var vorher = Directory.Exists(basis) ? Directory.GetDirectories(basis).Length : -1;

        TempCleanup.RaeumeAlteEntpackungenWeg();

        var nachher = Directory.Exists(basis) ? Directory.GetDirectories(basis).Length : -1;
        Assert.Equal(vorher, nachher);
    }
}
