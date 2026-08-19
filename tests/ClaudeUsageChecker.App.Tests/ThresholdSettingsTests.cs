using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Tray;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the configurable thresholds: validation of the input, saving, and
/// the effect on the icon colour.
/// </summary>
public class ThresholdSettingsTests
{
    [Fact]
    public void ValidateThresholds_UsualValuesAreFine()
    {
        Assert.Null(AppSettings.ValidateThresholds(75, 90));
    }

    [Fact]
    public void ValidateThresholds_AWarningAboveTheCriticalIsRejected()
    {
        // It would never take effect - the icon would jump straight to red.
        Assert.NotNull(AppSettings.ValidateThresholds(95, 90));
    }

    [Fact]
    public void ValidateThresholds_EqualValuesAreRejected()
    {
        Assert.NotNull(AppSettings.ValidateThresholds(90, 90));
    }

    [Theory]
    [InlineData(0, 90)]
    [InlineData(75, 101)]
    [InlineData(-5, 90)]
    public void ValidateThresholds_ValuesOutsideTheRangeAreRejected(double warnung, double kritisch)
    {
        Assert.NotNull(AppSettings.ValidateThresholds(warnung, kritisch));
    }

    [AvaloniaFact]
    public void TheWindowShowsTheStoredThresholds()
    {
        using var file = new TemporarySettings();
        var settings = new AppSettings { WarningThreshold = 60, CriticalThreshold = 80 };

        var window = CreateWindow(file, settings);

        Assert.Equal(60m, window.FindControl<NumericUpDown>("WarningThresholdBox")!.Value);
        Assert.Equal(80m, window.FindControl<NumericUpDown>("CriticalThresholdBox")!.Value);
    }

    [AvaloniaFact]
    public void ChangedThresholdsAreSaved()
    {
        using var file = new TemporarySettings();
        var window = CreateWindow(file, new AppSettings());

        window.FindControl<NumericUpDown>("WarningThresholdBox")!.Value = 50m;
        window.FindControl<NumericUpDown>("CriticalThresholdBox")!.Value = 85m;
        Save(window);

        var saved = file.Store.Load();
        Assert.Equal(50d, saved.WarningThreshold);
        Assert.Equal(85d, saved.CriticalThreshold);
    }

    [AvaloniaFact]
    public void AnImpossibleCombinationIsNotSaved()
    {
        using var file = new TemporarySettings();
        var window = CreateWindow(file, new AppSettings());

        window.FindControl<NumericUpDown>("WarningThresholdBox")!.Value = 95m;
        window.FindControl<NumericUpDown>("CriticalThresholdBox")!.Value = 90m;
        Save(window);

        Assert.True(window.FindControl<TextBlock>("ThresholdHint")!.IsVisible);
        Assert.False(File.Exists(file.Path));
    }

    /// <summary>
    /// Saving the settings rebuilds the whole object. If the recorded version is
    /// forgotten in the process, the summary of changes would come back on the
    /// next start - although it has been shown already.
    /// </summary>
    [AvaloniaFact]
    public void TheRecordedVersionSurvivesSaving()
    {
        using var file = new TemporarySettings();
        var settings = new AppSettings { LastRunVersion = "0.5.0", InstallPromptShown = true };

        var window = CreateWindow(file, settings);
        Save(window);

        var saved = file.Store.Load();
        Assert.Equal("0.5.0", saved.LastRunVersion);
        Assert.True(saved.InstallPromptShown);
    }

    [Fact]
    public void TheThresholdsDecideTheIconColour()
    {
        var zustand = new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(55, DateTimeOffset.UtcNow.AddHours(2)),
                Weekly = new UsageWindow(20, DateTimeOffset.UtcNow.AddDays(3)),
                RetrievedAt = DateTimeOffset.UtcNow
            }
        };

        Assert.Equal(TrayIconSeverity.Normal, TrayIconSeverityResolver.Resolve(zustand, 75, 90));
        Assert.Equal(TrayIconSeverity.Warning, TrayIconSeverityResolver.Resolve(zustand, 50, 90));
        Assert.Equal(TrayIconSeverity.Critical, TrayIconSeverityResolver.Resolve(zustand, 30, 50));
    }

    private static void Save(Window window)
    {
        window.Show();
        window.FindControl<Button>("SaveButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Hide();
    }

    /// <summary>
    /// Builds the window so that saving touches nothing outside the test - in
    /// particular not the user's autostart entry.
    /// </summary>
    private static SettingsWindow CreateWindow(TemporarySettings file, AppSettings settings) =>
        new(file.Store, settings, applyAutostart: _ => { });

    /// <summary>Settings in a temporary file, so that tests leave the user's alone.</summary>
    private sealed class TemporarySettings : IDisposable
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
