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
        if (WritesDisabled)
        {
            return;
        }

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

    /// <summary>
    /// Keeps every write away from the real machine. Set by the test assembly
    /// before anything runs, never by the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A test that pressed "save" in the settings window with the box unticked
    /// deleted the autostart entry of whoever ran the suite - every run, for
    /// weeks. The settings file kept saying yes, because the test wrote its
    /// settings to a temporary file, so the box stayed ticked over an entry that
    /// was gone. It looked exactly like an update dropping the application from
    /// autostart, since the suite runs right before every release.
    /// </para>
    /// <para>
    /// The rule that tests inject a switch of their own was written down, and one
    /// test still did not. A rule that has to be remembered at every call is a
    /// rule that will be forgotten once; this makes forgetting harmless.
    /// </para>
    /// </remarks>
    internal static bool WritesDisabled { get; set; }

    /// <summary>
    /// Puts the entry back where the settings want autostart and it is missing,
    /// or points somewhere other than <paramref name="path"/>.
    /// </summary>
    /// <returns>Whether anything had to be written.</returns>
    /// <remarks>
    /// <para>
    /// The settings and the system can drift apart without the application
    /// doing anything: a test once removed the entry, a clean-up tool may, and
    /// whatever did it, the box in the settings still says yes. Checking at
    /// every start closes that gap - the user's choice is what the settings
    /// say, and the system is brought back in line with it.
    /// </para>
    /// <para>
    /// Only where something is actually wrong. On macOS writing the agent also
    /// asks launchd to load it, which is nothing to do on every start for an
    /// agent that is already there. An entry switched off in the Windows task
    /// manager is left alone as well: that switch lives in a key of its own,
    /// and the Run value beside it stays untouched.
    /// </para>
    /// </remarks>
    public static bool Restore(string path)
    {
        if (WritesDisabled || string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            if (!NeedsRestoring(ReadWindows(), path))
            {
                return false;
            }

            ApplyWindows(enabled: true, path);
            return true;
        }

        if (OperatingSystem.IsMacOS() && !System.IO.File.Exists(ClaudeUsageChecker.App.Services.MacOsLaunchAgent.PlistPath))
        {
            ClaudeUsageChecker.App.Services.MacOsLaunchAgent.Apply(enabled: true, path);
            return true;
        }

        return false;
    }

    /// <summary>What the Run value holds for a program: its path, in quotes.</summary>
    /// <remarks>Quoted, because the profile path may well contain a space.</remarks>
    internal static string RunValueFor(string path) => $"\"{path}\"";

    /// <summary>Whether the Run value found is not the one the program needs.</summary>
    internal static bool NeedsRestoring(string? current, string path) =>
        !string.Equals(current, RunValueFor(path), StringComparison.OrdinalIgnoreCase);

    [SupportedOSPlatform("windows")]
    private static string? ReadWindows()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
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
                var target = path ?? Environment.ProcessPath;
                if (!string.IsNullOrEmpty(target))
                {
                    key.SetValue(ValueName, RunValueFor(target));
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
