using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Registers or unregisters the application for automatic startup. On Windows
/// through the Run key of the current user - no interference with system-wide
/// settings, no elevated rights needed.
/// </summary>
internal static class AutostartManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClaudeUsageChecker";

    /// <param name="path">
    /// The path to register. Without one, the current path is used - but when
    /// installing, the target path has to be registered, not the one the
    /// application happens to be running from.
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
            // A failed autostart entry must not disturb the application.
        }
    }
}
