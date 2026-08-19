using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Bringt die Anwendung an einen festen Platz im Benutzerprofil und richtet den
/// Autostart ein.
/// </summary>
/// <remarks>
/// <para>
/// Der Grund ist nicht Ordnungsliebe: Der Autostart und die Anheftung im
/// Infobereich haengen beide am Pfad der ausfuehrbaren Datei. Liegt sie im
/// Download-Ordner, bricht beides, sobald dort aufgeraeumt wird. Auch der
/// Selbstaustausch schreibt an genau diesen Pfad.
/// </para>
/// <para>
/// Kopiert wird nur nach Rueckfrage. Ein heruntergeladenes Programm, das sich
/// ungefragt anderswohin schreibt und in den Autostart eintraegt, waere
/// uebergriffig - unabhaengig davon, wie nuetzlich es ist.
/// </para>
/// </remarks>
public static class SelfInstaller
{
    /// <summary>
    /// Verzeichnis, in dem die Anwendung dauerhaft liegen soll.
    /// </summary>
    /// <remarks>
    /// <c>%LOCALAPPDATA%\Programs</c> ist der von Windows vorgesehene Ort fuer
    /// Anwendungen, die ohne Administratorrechte auskommen - dort liegen etwa
    /// auch VS Code und Signal. Das haelt die Wurzel des Benutzerprofils frei,
    /// wo neben Dokumenten und Downloads niemand Programme erwartet.
    /// </remarks>
    public static string TargetDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "ClaudeUsageChecker");

    /// <summary>Vollstaendiger Zielpfad der ausfuehrbaren Datei.</summary>
    public static string TargetPath { get; } = Path.Combine(TargetDirectory, "ClaudeUsageChecker.exe");

    /// <summary>Ob die laufende Fassung bereits am Zielort liegt.</summary>
    public static bool IsInstalled =>
        Environment.ProcessPath is { } pfad
        && string.Equals(Path.GetFullPath(pfad), TargetPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ob es sinnvoll ist, die Installation anzubieten: nur bei einer
    /// veroeffentlichten Einzeldatei, die noch nicht am Zielort liegt.
    /// </summary>
    public static bool ShouldOffer => UpdateInstaller.IsSupported && !IsInstalled;

    /// <summary>
    /// Kopiert die laufende Datei an den Zielort, richtet den Autostart ein und
    /// startet sie von dort. Bei Erfolg muss der Aufrufer sich beenden - die
    /// neue Instanz wartet bereits darauf.
    /// </summary>
    public static InstallResult Install()
    {
        if (Environment.ProcessPath is not { Length: > 0 } quelle)
        {
            return InstallResult.Failed("Der eigene Speicherort ließ sich nicht ermitteln.");
        }

        if (IsInstalled)
        {
            return new InstallResult(true, "Die Anwendung liegt bereits am Zielort.");
        }

        try
        {
            Directory.CreateDirectory(TargetDirectory);
            File.Copy(quelle, TargetPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InstallResult.Failed("Das Kopieren ist fehlgeschlagen: " + ex.Message);
        }

        // Der Autostart zeigt bewusst auf den Zielpfad, nicht auf den aktuellen -
        // sonst zeigte er weiterhin in den Download-Ordner.
        AutostartManager.Apply(enabled: true, TargetPath);

        var start = new ProcessStartInfo(TargetPath) { UseShellExecute = false };
        start.ArgumentList.Add(UpdateInstaller.WaitArgument);
        start.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

        try
        {
            Process.Start(start);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return InstallResult.Failed("Die kopierte Fassung ließ sich nicht starten: " + ex.Message);
        }

        return new InstallResult(true, "Installiert. Die Anwendung startet gerade von ihrem neuen Platz.");
    }
}
