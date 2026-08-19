using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Removes the extraction folders of earlier versions from the temporary
/// directory.
/// </summary>
/// <remarks>
/// <para>
/// A compressed single file cannot load its native libraries from the bundle -
/// the .NET runtime extracts them at startup to
/// <c>%TEMP%\.net\&lt;application&gt;\&lt;id&gt;</c>. The id depends on the content, so every
/// new version creates a folder of its own. Nothing is cleaned up in the
/// process: left alone, the directory grows by some 16 MB per update.
/// </para>
/// <para>
/// The application's own folder is recognised from the loaded modules, not
/// through <c>AppContext.BaseDirectory</c>. For a single file the latter points at
/// the directory of the executable rather than at the extraction - relying on it
/// deletes the very folder the running application loads its libraries from.
/// </para>
/// <para>
/// Where the own folder cannot be determined, nothing is deleted. A cleaner
/// that strikes when in doubt does more damage than the disk space it frees.
/// </para>
/// </remarks>
public static class TempCleanup
{
    private const string AppFolderPrefix = "ClaudeUsageChecker";

    /// <summary>Cleans up and reports how many folders were removed.</summary>
    public static int RemoveStaleExtractions()
    {
        var basis = Path.Combine(Path.GetTempPath(), ".net");
        if (!Directory.Exists(basis))
        {
            return 0;
        }

        var eigene = FindeEigeneEntpackungen(basis);
        if (eigene.Count == 0)
        {
            // Without knowing our own folder, better to touch nothing.
            return 0;
        }

        var entfernt = 0;

        foreach (var anwendungsordner in VerzeichnisseSicher(basis))
        {
            if (!Path.GetFileName(anwendungsordner).StartsWith(AppFolderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var entpackung in VerzeichnisseSicher(anwendungsordner))
            {
                if (eigene.Contains(Normalisiere(entpackung)))
                {
                    continue;
                }

                if (LoescheSicher(entpackung))
                {
                    entfernt++;
                }
            }

            if (VerzeichnisseSicher(anwendungsordner).Length == 0
                && DateienSicher(anwendungsordner).Length == 0)
            {
                LoescheSicher(anwendungsordner);
            }
        }

        return entfernt;
    }

    /// <summary>
    /// The extraction folders this process has actually loaded libraries from.
    /// That is the reliable answer to what is in use.
    /// </summary>
    private static HashSet<string> FindeEigeneEntpackungen(string basis)
    {
        var gefunden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var basisNormalisiert = Normalisiere(basis);

        try
        {
            foreach (ProcessModule modul in Process.GetCurrentProcess().Modules)
            {
                if (modul.FileName is not { Length: > 0 } datei)
                {
                    continue;
                }

                var ordner = Path.GetDirectoryName(datei);
                if (ordner is null || !Normalisiere(ordner).StartsWith(basisNormalisiert, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                gefunden.Add(Normalisiere(ordner));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            // No module list, no answer - then nothing is cleaned up.
        }

        return gefunden;
    }

    private static string Normalisiere(string pfad) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(pfad));

    private static string[] VerzeichnisseSicher(string pfad)
    {
        try
        {
            return Directory.GetDirectories(pfad);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string[] DateienSicher(string pfad)
    {
        try
        {
            return Directory.GetFiles(pfad);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool LoescheSicher(string pfad)
    {
        try
        {
            Directory.Delete(pfad, recursive: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // In use or locked - try again on the next start.
            return false;
        }
    }
}
