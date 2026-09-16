using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The settings of the usage notices: what is shown, what is saved, and what is
/// remembered across a restart.
/// </summary>
public class AlertSettingsTests
{
    [AvaloniaFact]
    public void TheWindowShowsTheStoredChoices()
    {
        using var file = new TemporaryFile();
        var settings = new AppSettings
        {
            AlertOnWarning = false,
            AlertOnCritical = true,
            AlertOnExhausted = false,
            AlertRequiresAcknowledgement = false,
            AlertStaysOnTop = false,
            AlertAutoCloseSeconds = 45
        };

        var window = CreateWindow(file, settings);

        Assert.False(window.FindControl<CheckBox>("AlertOnWarningBox")!.IsChecked);
        Assert.True(window.FindControl<CheckBox>("AlertOnCriticalBox")!.IsChecked);
        Assert.False(window.FindControl<CheckBox>("AlertOnExhaustedBox")!.IsChecked);
        Assert.False(window.FindControl<CheckBox>("AlertAcknowledgeBox")!.IsChecked);
        Assert.False(window.FindControl<CheckBox>("AlertOnTopBox")!.IsChecked);
        Assert.Equal(45m, window.FindControl<NumericUpDown>("AlertAutoCloseBox")!.Value);
    }

    [AvaloniaFact]
    public void ChangedChoicesAreSaved()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file, new AppSettings());

        window.FindControl<CheckBox>("AlertOnWarningBox")!.IsChecked = false;
        window.FindControl<CheckBox>("AlertAcknowledgeBox")!.IsChecked = false;
        window.FindControl<CheckBox>("AlertOnTopBox")!.IsChecked = false;
        window.FindControl<NumericUpDown>("AlertAutoCloseBox")!.Value = 60m;
        Save(window);

        var saved = file.Store.Load();
        Assert.False(saved.AlertOnWarning);
        Assert.True(saved.AlertOnCritical);
        Assert.True(saved.AlertOnExhausted);
        Assert.False(saved.AlertRequiresAcknowledgement);
        Assert.False(saved.AlertStaysOnTop);
        Assert.Equal(60, saved.AlertAutoCloseSeconds);
    }

    /// <summary>
    /// A time to close by itself means nothing for a notice that waits - the box
    /// says so by being unavailable rather than by disappearing.
    /// </summary>
    [AvaloniaFact]
    public void TheTimeToCloseIsOnlyAvailableForANoticeThatDoesNotWait()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file, new AppSettings());
        var acknowledge = window.FindControl<CheckBox>("AlertAcknowledgeBox")!;
        var autoClose = window.FindControl<NumericUpDown>("AlertAutoCloseBox")!;

        var whileWaiting = autoClose.IsEnabled;
        acknowledge.IsChecked = false;

        Assert.False(whileWaiting);
        Assert.True(autoClose.IsEnabled);
    }

    /// <summary>The notice follows the thresholds of the icon, not a pair of its own.</summary>
    [Fact]
    public void TheRulesUseTheThresholdsOfTheIcon()
    {
        var settings = new AppSettings { WarningThreshold = 60, CriticalThreshold = 80, AlertOnExhausted = false };

        var rules = settings.AlertRules;

        Assert.Equal(60d, rules.WarningThreshold);
        Assert.Equal(80d, rules.CriticalThreshold);
        Assert.False(rules.OnExhausted);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(30, 30)]
    [InlineData(999999, 3600)]
    public void AnImpossibleTimeToCloseIsBroughtIntoRange(int stored, int expected)
    {
        var settings = new AppSettings { AlertAutoCloseSeconds = stored };

        Assert.Equal(TimeSpan.FromSeconds(expected), settings.AlertBehaviour.AutoClose);
    }

    [Fact]
    public void AFileFromAnEarlierVersionKeepsEveryNoticeSwitchedOn()
    {
        using var file = new TemporaryFile();
        File.WriteAllText(file.Path, """{ "pollIntervalSeconds": 300 }""");

        var loaded = file.Store.Load();

        Assert.True(loaded.AlertOnWarning);
        Assert.True(loaded.AlertOnCritical);
        Assert.True(loaded.AlertOnExhausted);
        Assert.True(loaded.AlertRequiresAcknowledgement);
        Assert.True(loaded.AlertStaysOnTop);
        Assert.Equal(30, loaded.AlertAutoCloseSeconds);
    }

    [Fact]
    public void WhatWasReportedCanBeReadBack()
    {
        using var file = new TemporaryFile();
        var store = new AlertMemoryStore(file.Path);
        var reset = new DateTimeOffset(2026, 9, 20, 3, 0, 0, TimeSpan.Zero);

        store.Save(new Dictionary<string, UsageAlertMark>
        {
            ["session"] = new(UsageAlertLevel.Critical, reset),
            ["weekly:Fable"] = new(UsageAlertLevel.Warning, reset.AddDays(2))
        });

        var loaded = store.Load();

        Assert.Equal(new UsageAlertMark(UsageAlertLevel.Critical, reset), loaded["session"]);
        Assert.Equal(UsageAlertLevel.Warning, loaded["weekly:Fable"].Level);
    }

    /// <summary>
    /// The stage is stored by name: readable in the file, and immune to the
    /// order of the enumeration changing one day.
    /// </summary>
    [Fact]
    public void TheStageIsStoredByName()
    {
        using var file = new TemporaryFile();
        var store = new AlertMemoryStore(file.Path);

        store.Save(new Dictionary<string, UsageAlertMark>
        {
            ["session"] = new(UsageAlertLevel.Exhausted, DateTimeOffset.UtcNow)
        });

        Assert.Contains("\"Exhausted\"", File.ReadAllText(file.Path), StringComparison.Ordinal);
    }

    [Fact]
    public void ADamagedMemoryIsForgottenRatherThanFatal()
    {
        using var file = new TemporaryFile();
        File.WriteAllText(file.Path, "{ not json");

        Assert.Empty(new AlertMemoryStore(file.Path).Load());
    }

    private static void Save(Window window)
    {
        window.Show();
        window.FindControl<Button>("SaveButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Hide();
    }

    /// <summary>Saving touches nothing outside the test - the autostart entry least of all.</summary>
    private static SettingsWindow CreateWindow(TemporaryFile file, AppSettings settings) =>
        new(file.Store, settings, applyAutostart: _ => { });

    private sealed class TemporaryFile : IDisposable
    {
        private SettingsStore? _store;

        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");

        public SettingsStore Store => _store ??= new SettingsStore(Path);

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
