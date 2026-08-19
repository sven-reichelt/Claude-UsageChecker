using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Entfernt die Entpackungsordner frueherer Fassungen aus dem
/// Temporaerverzeichnis.
/// </summary>
/// <remarks>
/// <para>
/// Eine komprimierte Einzeldatei kann ihre nativen Bibliotheken nicht aus dem
/// Buendel laden - die .NET-Laufzeit packt sie beim Start nach
/// <c>%TEMP%\.net\&lt;Anwendung&gt;\&lt;Kennung&gt;</c> aus. Die Kennung haengt am Inhalt,
/// jede neue Fassung legt also einen eigenen Ordner an. Aufgeraeumt wird dabei
/// nichts: Ohne Zutun waechst das Verzeichnis mit jeder Aktualisierung um rund
/// 16 MB.
/// </para>
/// <para>
/// Der eigene Ordner wird an den geladenen Modulen erkannt, nicht ueber
/// <c>AppContext.BaseDirectory</c>. Letzteres zeigt bei einer Einzeldatei auf das
/// Verzeichnis der ausfuehrbaren Datei und nicht auf die Entpackung - wer sich
/// darauf verlaesst, loescht den Ordner, aus dem die laufende Anwendung ihre
/// Bibliotheken laedt.
/// </para>
/// <para>
/// Laesst sich der eigene Ordner nicht bestimmen, wird nichts geloescht. Ein
/// Aufraeumer, der im Zweifel zuschlaegt, richtet mehr Schaden an als der
/// belegte Platz.
/// </para>
/// </remarks>
public static class TempCleanup
{
    private const string AppFolderPrefix = "ClaudeUsageChecker";

    /// <summary>Raeumt auf und meldet, wie viele Ordner entfernt wurden.</summary>
    public static int RaeumeAlteEntpackungenWeg()
    {
        var basis = Path.Combine(Path.GetTempPath(), ".net");
        if (!Directory.Exists(basis))
        {
            return 0;
        }

        var eigene = FindeEigeneEntpackungen(basis);
        if (eigene.Count == 0)
        {
            // Ohne Kenntnis des eigenen Ordners lieber nichts anfassen.
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
    /// Die Entpackungsordner, aus denen dieser Prozess gerade Bibliotheken
    /// geladen hat. Das ist die verlaessliche Auskunft darueber, was in
    /// Benutzung ist.
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
            // Ohne Modulliste keine Aussage - dann wird nicht aufgeraeumt.
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
            // Belegt oder gesperrt - beim naechsten Start noch einmal.
            return false;
        }
    }
}
