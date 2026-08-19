using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Ergebnis eines Einspielversuchs.</summary>
public sealed record InstallResult(bool Succeeded, string Message)
{
    public static InstallResult Failed(string message) => new(false, message);
}

/// <summary>
/// Laedt eine neue Fassung herunter und ersetzt die laufende Datei.
/// </summary>
/// <remarks>
/// <para>
/// Hier wird bewusst Fremdcode heruntergeladen und ausgefuehrt - eine
/// Abweichung von der urspruenglichen Zurueckhaltung, getroffen weil ein
/// blosser Hinweis in der Praxis liegen bleibt. Die Absicherung liegt deshalb
/// auf der Pruefsumme: Ohne veroeffentlichte SHA-256-Summe und ohne
/// Uebereinstimmung wird nichts eingespielt. Die Adressen stammen aus der
/// GitHub-Antwort zu genau diesem Repository, nicht aus zusammengesetzten
/// Zeichenketten.
/// </para>
/// <para>
/// Windows laesst eine laufende Datei nicht ueberschreiben, wohl aber
/// umbenennen. Genau darauf beruht das Verfahren: umbenennen, neue Datei an den
/// alten Platz legen, neue Fassung starten, selbst beenden. Die neue Instanz
/// raeumt die umbenannte Altfassung beim Start weg.
/// </para>
/// </remarks>
public sealed class UpdateInstaller(HttpClient httpClient)
{
    /// <summary>Endung der beiseitegelegten Altfassung.</summary>
    public const string BackupSuffix = ".alt";

    /// <summary>Schalter, mit dem die neue Fassung auf das Ende der alten wartet.</summary>
    public const string WaitArgument = "--nach-update";

    /// <summary>
    /// Ob sich die laufende Fassung selbst ersetzen kann. Das setzt eine
    /// Veroeffentlichung als Einzeldatei voraus - im Entwicklungsstand liegen
    /// Dutzende Dateien nebeneinander, die einzeln zu tauschen waeren.
    /// </summary>
    /// <remarks>
    /// Erkannt wird das am Fehlen der gleichnamigen Bibliothek neben der
    /// ausfuehrbaren Datei. Das trifft genau die Frage, auf die es ankommt:
    /// Genuegt es, diese eine Datei zu tauschen? Ueber
    /// <c>Assembly.Location</c> waere es zwar auch zu ermitteln, doch dessen
    /// Verhalten in Einzeldateien gilt zu Recht als Stolperstein.
    /// </remarks>
    public static bool IsSupported
    {
        get
        {
            if (!OperatingSystem.IsWindows() || Environment.ProcessPath is not { Length: > 0 } pfad)
            {
                return false;
            }

            return !File.Exists(Path.ChangeExtension(pfad, ".dll"));
        }
    }

    /// <summary>
    /// Laedt die neue Fassung, prueft sie und legt sie an die Stelle der alten.
    /// Bei Erfolg laeuft bereits die neue Instanz - der Aufrufer muss sich
    /// danach beenden.
    /// </summary>
    public async Task<InstallResult> InstallAsync(
        UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (!IsSupported)
        {
            return InstallResult.Failed(
                "Diese Fassung kann sich nicht selbst ersetzen. "
                + "Bitte die neue Version von der Release-Seite laden.");
        }

        if (update is not { DownloadUrl: { } downloadUrl, ChecksumUrl: { } checksumUrl })
        {
            return InstallResult.Failed(
                "Zur neuen Version fehlt die Datei oder die Pruefsumme. "
                + "Ohne beides wird nichts eingespielt.");
        }

        var eigenerPfad = Environment.ProcessPath!;
        var temp = Path.Combine(Path.GetTempPath(), $"ClaudeUsageChecker-{Guid.NewGuid():N}.exe");

        try
        {
            await LadeAsync(downloadUrl, temp, cancellationToken).ConfigureAwait(false);

            var erwartet = await LadePruefsummeAsync(checksumUrl, cancellationToken).ConfigureAwait(false);
            if (erwartet is null)
            {
                return InstallResult.Failed("Die Pruefsumme war nicht lesbar. Es wird nichts eingespielt.");
            }

            var tatsaechlich = await BerechnePruefsummeAsync(temp, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(erwartet, tatsaechlich, StringComparison.OrdinalIgnoreCase))
            {
                return InstallResult.Failed(
                    "Die Pruefsumme der heruntergeladenen Datei stimmt nicht. "
                    + "Sie wird verworfen und nicht ausgefuehrt.");
            }

            return Tausche(eigenerPfad, temp);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return InstallResult.Failed("Die neue Fassung konnte nicht geladen werden: " + ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InstallResult.Failed("Die neue Fassung konnte nicht gespeichert werden: " + ex.Message);
        }
        finally
        {
            Loesche(temp);
        }
    }

    private async Task LadeAsync(Uri url, string ziel, CancellationToken cancellationToken)
    {
        using var response = await httpClient
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var quelle = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var datei = File.Create(ziel);
        await quelle.CopyToAsync(datei, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> LadePruefsummeAsync(Uri url, CancellationToken cancellationToken)
    {
        var inhalt = await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return LiesPruefsumme(inhalt);
    }

    /// <summary>
    /// Zieht die Summe aus einer Datei der Form "&lt;hash&gt;  &lt;dateiname&gt;".
    /// </summary>
    internal static string? LiesPruefsumme(string inhalt)
    {
        if (string.IsNullOrWhiteSpace(inhalt))
        {
            return null;
        }

        var erstesWort = inhalt.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];

        // Eine SHA-256-Summe hat 64 Stellen in Hexadezimalschreibweise.
        return erstesWort.Length == 64 && IsHex(erstesWort) ? erstesWort.ToLowerInvariant() : null;
    }

    private static bool IsHex(string wert)
    {
        foreach (var c in wert)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<string> BerechnePruefsummeAsync(string pfad, CancellationToken cancellationToken)
    {
        await using var datei = File.OpenRead(pfad);
        var hash = await SHA256.HashDataAsync(datei, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Legt die laufende Datei beiseite, setzt die neue an ihren Platz und
    /// startet sie. Scheitert der zweite Schritt, wird der erste
    /// zurueckgenommen - sonst bliebe kein lauffaehiges Programm zurueck.
    /// </summary>
    private static InstallResult Tausche(string eigenerPfad, string neueDatei)
    {
        var beiseite = eigenerPfad + BackupSuffix;
        Loesche(beiseite);

        File.Move(eigenerPfad, beiseite);

        try
        {
            File.Move(neueDatei, eigenerPfad);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            File.Move(beiseite, eigenerPfad);
            return InstallResult.Failed("Die neue Fassung liess sich nicht einsetzen: " + ex.Message);
        }

        var start = new ProcessStartInfo(eigenerPfad) { UseShellExecute = false };
        start.ArgumentList.Add(WaitArgument);
        start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Process.Start(start);

        return new InstallResult(true, "Die neue Fassung wurde eingespielt und startet gerade.");
    }

    /// <summary>
    /// Entfernt eine beiseitegelegte Altfassung. Wird beim Start aufgerufen,
    /// denn erst dann laeuft sie nicht mehr.
    /// </summary>
    public static void RaeumeAltfassungWeg()
    {
        if (Environment.ProcessPath is { } pfad)
        {
            Loesche(pfad + BackupSuffix);
        }
    }

    private static void Loesche(string pfad)
    {
        try
        {
            if (File.Exists(pfad))
            {
                File.Delete(pfad);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Beim naechsten Start noch einmal - kein Grund, hier zu scheitern.
        }
    }
}
