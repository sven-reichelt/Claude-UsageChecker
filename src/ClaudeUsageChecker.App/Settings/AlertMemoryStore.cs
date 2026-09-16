using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Settings;

/// <summary>
/// Remembers across a restart which usage notices have already been shown.
/// </summary>
/// <remarks>
/// <para>
/// Without it every start would begin knowing nothing, and a weekly limit in the
/// yellow would be announced again each morning by the autostart - a notice that
/// has to be confirmed, about something said the day before.
/// </para>
/// <para>
/// A file of its own rather than a field in the settings. The settings window
/// rebuilds the settings from its controls when saving, and state that belongs
/// to none of them has to be carried across by hand there; forgetting it once
/// would bring back every notice. Here nothing but this class writes.
/// </para>
/// <para>
/// What it holds is a stage and a reset time per limit - no token, no account,
/// nothing that leaves the machine.
/// </para>
/// </remarks>
public sealed class AlertMemoryStore(string? path = null)
{
    private readonly string _path = path ?? AppPaths.AlertMemoryFile;

    public Dictionary<string, UsageAlertMark> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, AlertMemoryJsonContext.Default.DictionaryStringUsageAlertMark) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Forgetting costs at most one notice too many; refusing to start
            // over a damaged file would cost the application.
            return [];
        }
    }

    public void Save(IReadOnlyDictionary<string, UsageAlertMark> marks)
    {
        ArgumentNullException.ThrowIfNull(marks);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var copy = new Dictionary<string, UsageAlertMark>(marks, StringComparer.Ordinal);
        File.WriteAllText(_path, JsonSerializer.Serialize(copy, AlertMemoryJsonContext.Default.DictionaryStringUsageAlertMark));
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Dictionary<string, UsageAlertMark>))]
internal sealed partial class AlertMemoryJsonContext : JsonSerializerContext;
