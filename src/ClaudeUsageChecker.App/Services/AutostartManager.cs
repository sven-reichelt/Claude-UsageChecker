using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Traegt die Anwendung fuer den automatischen Start ein bzw. aus.
/// Unter Windows ueber den Run-Schluessel des aktuellen Nutzers -
/// kein Eingriff in systemweite Einstellungen, keine erhoehten Rechte noetig.
/// </summary>
internal static class AutostartManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClaudeUsageChecker";

    /// <param name="path">
    /// Der einzutragende Pfad. Ohne Angabe der aktuelle - beim Installieren muss
    /// aber der Zielpfad eingetragen werden, nicht der, von dem gerade
    /// gestartet wurde.
    /// </param>
    public static void Apply(bool enabled, string? path = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ApplyWindows(enabled, path);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyWindows(bool enabled, string? path)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                var ziel = path ?? Environment.ProcessPath;
                if (!string.IsNullOrEmpty(ziel))
                {
                    key.SetValue(ValueName, $"\"{ziel}\"");
                }
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Ein fehlgeschlagener Autostart-Eintrag darf die Anwendung nicht stoeren.
        }
    }
}
