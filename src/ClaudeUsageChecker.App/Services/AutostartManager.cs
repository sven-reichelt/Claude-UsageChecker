using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Registers or unregisters the application for automatic startup: on Windows
/// through the Run key of the current user, on macOS through a launch agent.
/// Neither touches system-wide settings, and neither needs elevated rights.
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
        if (OperatingSystem.IsWindows())
        {
            ApplyWindows(enabled, path);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            ClaudeUsageChecker.App.Services.MacOsLaunchAgent.Apply(enabled, path);
        }
    }

    /// <summary>Whether autostart can be set up on the running system at all.</summary>
    public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

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
                var target = path ?? Environment.ProcessPath;
                if (!string.IsNullOrEmpty(target))
                {
                    key.SetValue(ValueName, $"\"{target}\"");
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
