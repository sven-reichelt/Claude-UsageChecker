using System;
using System.IO;
using System.Text.Json;

namespace ClaudeUsageChecker.App.Settings;

/// <summary>Loads and saves the user settings as JSON inside the local profile.</summary>
public sealed class SettingsStore(string? path = null)
{
    private readonly string _path = path ?? AppPaths.SettingsFile;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupted settings must not prevent the start.
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings);
        File.WriteAllText(_path, json);
    }
}
