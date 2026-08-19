using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Result of an attempt to install.</summary>
public sealed record InstallResult(bool Succeeded, string Message)
{
    public static InstallResult Failed(string message) => new(false, message);
}

/// <summary>
/// Downloads a new version and replaces the running file.
/// </summary>
/// <remarks>
/// <para>
/// This deliberately downloads and executes foreign code - a departure from the
/// original restraint, made because a mere notice tends to be left lying around.
/// The safeguard therefore rests on the checksum: without a published SHA-256 sum
/// and without a match, nothing is installed. The addresses come from the GitHub
/// response for exactly this repository, not from strings pieced together.
/// </para>
/// <para>
/// Windows does not allow a running file to be overwritten, but it does allow
/// renaming. The procedure rests on exactly that: rename, put the new file in the
/// old place, start the new version, end this one. The new instance clears away
/// the renamed old version on startup.
/// </para>
/// </remarks>
public sealed class UpdateInstaller(HttpClient httpClient)
{
    /// <summary>Extension of the old version that has been set aside.</summary>
    public const string BackupSuffix = ".alt";

    /// <summary>Switch that makes the new version wait for the old one to end.</summary>
    public const string WaitArgument = "--nach-update";

    /// <summary>
    /// Whether the running version can replace itself. That requires a release
    /// as a single file - in a development build dozens of files sit side by
    /// side, each of which would have to be swapped.
    /// </summary>
    /// <remarks>
    /// It is recognised from the absence of the like-named library next to the
    /// executable. That answers precisely the question that matters: is swapping
    /// this one file enough? <c>Assembly.Location</c> could tell as well, but its
    /// behaviour in single files is rightly considered a pitfall.
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
    /// Downloads the new version, verifies it and puts it in place of the old
    /// one. On success the new instance is already running - the caller has to end
    /// itself afterwards.
    /// </summary>
    public async Task<InstallResult> InstallAsync(
        UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (!IsSupported)
        {
            return InstallResult.Failed(
                T.UpdaterNotSelfReplaceable);
        }

        if (update is not { DownloadUrl: { } downloadUrl, ChecksumUrl: { } checksumUrl })
        {
            return InstallResult.Failed(
                T.UpdaterMissingFileOrChecksum);
        }

        var eigenerPfad = Environment.ProcessPath!;
        var temp = Path.Combine(Path.GetTempPath(), $"ClaudeUsageChecker-{Guid.NewGuid():N}.exe");

        try
        {
            await LadeAsync(downloadUrl, temp, cancellationToken).ConfigureAwait(false);

            var erwartet = await LadePruefsummeAsync(checksumUrl, cancellationToken).ConfigureAwait(false);
            if (erwartet is null)
            {
                return InstallResult.Failed(T.UpdaterChecksumUnreadable);
            }

            var tatsaechlich = await BerechnePruefsummeAsync(temp, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(erwartet, tatsaechlich, StringComparison.OrdinalIgnoreCase))
            {
                return InstallResult.Failed(
                    T.UpdaterChecksumMismatch);
            }

            return Tausche(eigenerPfad, temp);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return InstallResult.Failed(T.UpdaterDownloadFailed(ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InstallResult.Failed(T.UpdaterSaveFailed(ex.Message));
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
    /// Extracts the sum from a file of the form "&lt;hash&gt;  &lt;filename&gt;".
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
            return InstallResult.Failed(T.UpdaterReplaceFailed(ex.Message));
        }

        var start = new ProcessStartInfo(eigenerPfad) { UseShellExecute = false };
        start.ArgumentList.Add(WaitArgument);
        start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Process.Start(start);

        return new InstallResult(true, T.UpdaterDone);
    }

    /// <summary>
    /// Entfernt eine beiseitegelegte Altfassung. Wird beim Start aufgerufen,
    /// denn erst dann laeuft sie nicht mehr.
    /// </summary>
    public static void RemovePreviousVersion()
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
