using System;
using System.IO;

namespace ClaudeUsageChecker.App;

/// <summary>Central storage locations of the application inside the user profile.</summary>
internal static class AppPaths
{
    public const string ProductName = "ClaudeUsageChecker";

    /// <summary>
    /// Machine-local data (logs, caches). LocalApplicationData on purpose, so
    /// that nothing travels into a roaming profile or a cloud backup.
    /// </summary>
    public static string LocalDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductName);

    /// <summary>File holding the user settings. Contains no secrets.</summary>
    public static string SettingsFile { get; } = Path.Combine(LocalDataDirectory, "settings.json");
}
