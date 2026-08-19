using System;
using System.IO;

namespace ClaudeUsageChecker.App;

/// <summary>Zentrale Ablageorte der Anwendung im Benutzerprofil.</summary>
internal static class AppPaths
{
    public const string ProductName = "ClaudeUsageChecker";

    /// <summary>
    /// Geraetelokale Daten (Protokolle, Zwischenspeicher). Bewusst LocalApplicationData,
    /// damit nichts in ein Roaming-Profil oder eine Cloud-Sicherung wandert.
    /// </summary>
    public static string LocalDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductName);

    /// <summary>Datei der Benutzereinstellungen. Enthaelt keine Geheimnisse.</summary>
    public static string SettingsFile { get; } = Path.Combine(LocalDataDirectory, "settings.json");
}
