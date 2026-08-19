using System.Text.Json;
using ClaudeUsageChecker.App.Settings;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks what actually ends up in the settings file.
/// </summary>
public class SettingsFileTests
{
    [Fact]
    public void TheFileContainsOnlyRealSettings()
    {
        // The computed PollInterval sat in the file for a while. It was never
        // read from there - but it looked like a second statement about the
        // polling interval that could contradict the first.
        using var file = new TemporaryFile();
        var store = new SettingsStore(file.Path);

        store.Save(new AppSettings { PollIntervalSeconds = 300 });

        using var geschrieben = JsonDocument.Parse(File.ReadAllText(file.Path));
        Assert.False(geschrieben.RootElement.TryGetProperty("PollInterval", out _));
        Assert.True(geschrieben.RootElement.TryGetProperty("pollIntervalSeconds", out _));
    }

    [Fact]
    public void WhatWasSavedCanBeReadBack()
    {
        using var file = new TemporaryFile();
        var store = new SettingsStore(file.Path);

        store.Save(new AppSettings
        {
            PollIntervalSeconds = 600,
            WarningThreshold = 60,
            CriticalThreshold = 85,
            LastRunVersion = "0.5.0",
            LaunchAtLogin = true
        });

        var gelesen = store.Load();

        Assert.Equal(600, gelesen.PollIntervalSeconds);
        Assert.Equal(60d, gelesen.WarningThreshold);
        Assert.Equal(85d, gelesen.CriticalThreshold);
        Assert.Equal("0.5.0", gelesen.LastRunVersion);
        Assert.True(gelesen.LaunchAtLogin);
    }

    /// <summary>
    /// A file from an older version does not know the new fields - the defaults
    /// have to take over there, rather than something falling to null or 0.
    /// </summary>
    [Fact]
    public void AFileWithoutTheNewFieldsKeepsTheDefaults()
    {
        using var file = new TemporaryFile();
        File.WriteAllText(file.Path, """
            {
              "pollIntervalSeconds": 300,
              "launchAtLogin": true,
              "installPromptShown": true,
              "checkForUpdates": true
            }
            """);

        var gelesen = new SettingsStore(file.Path).Load();

        Assert.Equal(75d, gelesen.WarningThreshold);
        Assert.Equal(90d, gelesen.CriticalThreshold);
        Assert.Null(gelesen.LastRunVersion);
    }

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
