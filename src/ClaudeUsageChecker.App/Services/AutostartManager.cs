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

    public static void Apply(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ApplyWindows(enabled);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyWindows(bool enabled)
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
                var path = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    key.SetValue(ValueName, $"\"{path}\"");
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
