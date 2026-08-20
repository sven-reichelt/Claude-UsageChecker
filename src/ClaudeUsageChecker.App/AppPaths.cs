using System;
using System.IO;

namespace ClaudeUsageChecker.App;

/// <summary>Central storage locations of the application inside the user profile.</summary>
internal static class AppPaths
{
    public const string ProductName = "ClaudeUsageChecker";

    /// <summary>
    /// Machine-local data (settings, logs). Local rather than roaming on
    /// purpose, so that nothing travels into a roaming profile or a cloud
    /// backup.
    /// </summary>
    /// <remarks>
    /// On macOS this is "Library/Application Support" and not what .NET calls
    /// LocalApplicationData - that maps to the Linux convention, ~/.local/share,
    /// where a Mac user has no reason to look and no tool expects to find
    /// anything.
    /// </remarks>
    public static string LocalDataDirectory { get; } = Path.Combine(
        OperatingSystem.IsMacOS()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support")
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductName);

    /// <summary>File holding the user settings. Contains no secrets.</summary>
    public static string SettingsFile { get; } = Path.Combine(LocalDataDirectory, "settings.json");
}
